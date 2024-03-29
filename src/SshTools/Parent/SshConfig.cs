﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentResults;
using SshTools.Line;
using SshTools.Line.Comment;
using SshTools.Line.Parameter;
using SshTools.Line.Parameter.Keyword;
using SshTools.Parent.Host;
using SshTools.Parent.Match;
using SshTools.Serialization;
using SshTools.Serialization.Parser;
using SshTools.Util;

namespace SshTools.Parent
{
    public class SshConfig : ParameterParent
    {
        //-----------------------------------------------------------------------//
        //                         Static Members getters
        //-----------------------------------------------------------------------//
        
        /// <summary>
        /// Parses a file by path.
        /// </summary>
        /// <param name="path">Path to file</param>
        /// <returns><see cref="Result{TValue}"/> of type <see cref="SshConfig"/></returns>
        public static Result<SshConfig> ReadFile(string path)
        {
            var readRes = Result.Try(() => File.ReadAllText(path));
            return readRes.IsFailed
                ? readRes.ToResult<SshConfig>()
                : DeserializeString(readRes.Value, path);
        }
        /// <summary>
        /// Parses the SSH config text.
        /// </summary>
        /// <param name="str">Config string</param>
        /// <param name="fileName">An optional path that will be provided to the config for serialization</param>
        /// <returns><see cref="Result{TValue}"/> of type <see cref="SshConfig"/></returns>
        public static Result<SshConfig> DeserializeString(string str, string fileName = null) => 
            Result.Try(() => Deserialized(str).ToConfig(fileName));
        
        /// <summary>
        /// Deserializes a string into a sequence of lines
        /// </summary>
        /// <param name="configString">The given string, that represents a ssh config</param>
        /// <returns>A sequence of lines representing <paramref name="configString"/></returns>
        /// <exception cref="ResultException">Thrown if something goes wrong while parsing</exception>
        /// <exception cref="Exception">Thrown if something goes wrong while parsing</exception>
        private static IEnumerable<ILine> Deserialized(string configString)
        {
            foreach (var l in configString.Split('\n'))
            {
                var line = l.Replace("\r", "");
                // Go for all comments (empty lines and comments, that are being stripped of their first #)
                if (LineParser.IsConfigComment(line))
                {
                    var comment = LineParser.TrimFront(line, out var spacingComment);

                    yield return new Comment(
                        comment.StartsWith("#")
                            ? comment.Substring(1, comment.Length - 1)
                            : comment,
                        spacingComment);
                    continue;
                }

                line = LineParser.TrimFront(line, out var spacingFront);

                line = LineParser.TrimKey(line, out var keyRes);
                if (keyRes.IsFailed)
                    throw new ResultException(keyRes.WithError($"While parsing line '{l}'"));

                var keyString = keyRes.Value;
                if (!SshTools.Settings.Has<Keyword>(keyString))
                    throw new Exception($"Unknown Keyword {keyRes.Value} while parsing line '{l}'");

                var key = SshTools.Settings.Get<Keyword>(keyString);

                line = LineParser.TrimSeparator(line, out var separatorRes);
                if (separatorRes.IsFailed)
                    throw new ResultException(separatorRes.WithError($"While parsing line '{l}'"));

                var spacingBack = LineParser.TrimArgument(line, out var argumentRes, out var quoted);

                var appearance = new ParameterAppearance(
                    spacingFront,
                    keyString,
                    separatorRes.Value,
                    quoted,
                    spacingBack
                );
                var paramRes = key.GetParameter(argumentRes, appearance);
                if (paramRes.IsFailed)
                    throw new ResultException(paramRes.WithError($"While parsing line '{l}'"));
                yield return paramRes.Value;
            }
        }
        
        //-----------------------------------------------------------------------//
        //                          SshConfig class
        //-----------------------------------------------------------------------//
        public string FileName { get; }

        /// <summary>
        /// Create a new SshConfig
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="parameters"></param>
        /// <exception cref="ArgumentException">Throws argument exception, if there is no HOST/MATCH keyword defined</exception>
        public SshConfig(string fileName = null, IList<ILine> parameters = null)
            : base(parameters)
        {
            FileName = fileName;
        }
        
        public new string Serialize(SerializeConfigOptions options = SerializeConfigOptions.DEFAULT)
        {
            var lines = new List<string>();
            lines.AddRange(this.Select(p => p.Serialize(options)));
            return string.Join(Environment.NewLine, lines);
        }

        public override object Clone()
        {
            var config = new SshConfig();
            foreach (var line in this) 
                config.Add((ILine)line.Clone());
            return config;
        }

        //-----------------------------------------------------------------------//
        //                   SshConfig specific functionality
        //-----------------------------------------------------------------------//
        
        /// <summary>
        /// Getter only, gets first host by looking for <paramref name="hostName"/>
        /// </summary>
        /// <param name="hostName">The name of the Host to be searched for</param>
        public HostNode this[string hostName] => this.Get(hostName);
        
        /// <summary>
        /// Gets a list of references to all <see cref="Node"/>s including the <see cref="SshConfig"/> at index [0]
        /// </summary>
        /// <param name="name">The name to be searched by</param>
        /// <param name="options">Searching options</param>
        /// <returns>A list of matching nodes with the config at position 0</returns>
        public IList<ParameterParent> GetAll(string name, MatchingOptions options = MatchingOptions.PATTERN)
        {
            var list = new List<ParameterParent> { this };
            foreach (var parameter in this.Matching(name, options))
            {
                if (parameter.Argument is Node parent)
                    list.Add(parent);
            }
            return list;
        }
        
        /// <summary>
        /// Gets a list of all nodes as reference including <see cref="SshConfig"/> at index [0]
        /// </summary>
        /// <returns>List of nodes</returns>
        public IList<ParameterParent> GetAll()
        {
            var list = new List<ParameterParent> { this};
            list.AddRange(this.Nodes());
            return list;
        }
    }
}