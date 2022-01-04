﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentResults;
using SshTools.Config.Extensions;
using SshTools.Config.Matching;
using SshTools.Config.Parents;
using SshTools.Config.Util;

namespace SshTools.Config.Parameters
{
    public static class ParameterParentExtensions
    {
        //-----------------------------------------------------------------------//
        //                      Basic Functions for Parameters
        //-----------------------------------------------------------------------//

        /// <summary>
        /// Checks if given sequence contains an entry with the given keyword
        /// </summary>
        /// <param name="parameters">A sequence of parameters to check on</param>
        /// <param name="keyword">The <see cref="Keyword"/> to be searched for</param>
        /// <returns>Whether the sequence contains an entry with this keyword</returns>
        public static bool Has(this IEnumerable<IParameter> parameters, Keyword keyword) => 
            parameters.Any(p => p.Is(keyword));
        
        /// <summary>
        /// Returns the argument of the first matching element of the given <paramref name="parameters"/>
        /// </summary>
        /// <param name="parameters">A sequence of parameters to get from</param>
        /// <param name="keyword">The <see cref="Keyword{T}"/> to be searched for</param>
        /// <typeparam name="T">Argument type</typeparam>
        /// <returns>First matching Argument as <typeparamref name="T"/></returns>
        public static T Get<T>(this IEnumerable<IParameter> parameters, Keyword<T> keyword) => 
            (T) parameters.Get((Keyword) keyword);

        /// <summary>
        /// Non generic way to get the argument by key as an object from the given sequence.
        /// Use only when necessary
        /// </summary>
        /// <param name="parameters">A sequence of parameters to get from</param>
        /// <param name="keyword">The <see cref="Keyword"/> to be searched for</param>
        /// <returns>First argument as <see cref="object"/></returns>
        internal static object Get(this IEnumerable<IParameter> parameters, Keyword keyword) =>
            parameters
                .Where(p => p.Is(keyword))
                .Select(p => p.Argument)
                .FirstOrDefault() ?? keyword.GetDefault();
        
        /// <summary>
        /// Returns the index of the given <paramref name="keyword"/>
        /// If the given sequence does not contain it the return will be -1
        /// </summary>
        /// <param name="parameters">A sequence of parameters to be searched</param>
        /// <param name="keyword">The <see cref="Keyword"/> to be searched for</param>
        /// <returns>Index of the element, -1 if not available</returns>
        public static int IndexOf(this IList<IParameter> parameters, Keyword keyword)
        {
            parameters.ThrowIfNull();
            keyword.ThrowIfNull();
            for (var i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].Keyword.Equals(keyword))
                    return i;
            }
            return -1;
        }
        
        /// <summary>
        /// Inserts a given value <typeparamref name="T"/> into the sequence of parameters at <paramref name="index"/>. 
        /// Dependent on <paramref name="ignoreCount"/> the method will check for an already existing one
        /// </summary>
        /// <param name="parameters">A sequence of parameters to be inserted to</param>
        /// <param name="index">The index to be inserted at</param>
        /// <param name="keyword">The keyword to insert</param>
        /// <param name="value">The value to be inserted</param>
        /// <param name="ignoreCount">Whether the insertion will be executed if a keyword, that is only allowed once,
        /// is already present</param>
        /// <typeparam name="T">The type of <paramref name="value"/></typeparam>
        /// <returns><see cref="Result{TValue}"/> with optionally value or reason for failure</returns>
        public static Result<T> Insert<T>(this IList<IParameter> parameters,
            int index, Keyword<T> keyword, T value, bool ignoreCount = false)
        {
            parameters.ThrowIfNull();
            keyword.ThrowIfNull();
            value.ThrowIfNull();
            if (index < -parameters.Count-1 || index > parameters.Count) Result.Fail(
                $"Index is out of bounds! May be in range of {-parameters.Count-1};{parameters.Count} but is {index}");
            if (!ignoreCount && !keyword.AllowMultiple && parameters.Has(keyword))
                return Result.Fail<T>($"Already containing entry with keyword {keyword}");
            var insertionRes = Result.Try(() => 
                parameters.Insert(
                    index >= 0 ? index : parameters.Count + index + 1, 
                    new Parameter<T>(keyword, value, ParameterAppearance.Default(keyword))));
            return insertionRes.IsSuccess
                ? Result.Ok(value)
                : insertionRes.ToResult<T>();
        }
        
        /// <summary>
        /// Sets the given <paramref name="value"/> to the sequence of parameters
        /// </summary>
        /// <param name="parameters">A sequence of parameters to set to</param>
        /// <param name="keyword">The <see cref="Keyword{T}"/> to be set</param>
        /// <param name="value">The value to be set</param>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <returns><see cref="Result{TValue}"/> with optionally value or reason for failure</returns>
        public static Result<T> Set<T>(this IList<IParameter> parameters, Keyword<T> keyword, T value)
        {
            parameters.ThrowIfNull();
            keyword.ThrowIfNull();
            value.ThrowIfNull();
            if (!parameters.Has(keyword)) 
                return parameters.Insert(0, keyword, value, true);
            var param = parameters.First(p => p.Is(keyword));
            param.Argument = value;
            return Result.Ok(value);
        }
        
        /// <summary>
        /// Removes the first matching entry in the given sequence
        /// </summary>
        /// <param name="parameters">A sequence of parameters to be removed from</param>
        /// <param name="keyword">The <see cref="Keyword"/> to be removed</param>
        /// <returns><see cref="Result{TValue}"/> with optional reason for failure</returns>
        public static Result Remove(this IList<IParameter> parameters, Keyword keyword)
        {
            parameters.ThrowIfNull();
            keyword.ThrowIfNull();
            var index = parameters.IndexOf(keyword);
            if (index < 0) return Result.Fail("Keyword not available");
            parameters.RemoveAt(index);
            return Result.Ok();
        }

        //-----------------------------------------------------------------------//
        //                    Advanced Functions for Parameters
        //-----------------------------------------------------------------------//
        
        /// <summary>
        /// Flattens the given sequence <paramref name="parameters"/>.
        /// Generates a list of all parameters, contained in <paramref name="parameters"/>;
        /// Creates new and empty nodes
        /// </summary>
        /// <param name="parameters">A sequence of parameters to flatten</param>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are the result of the flattening</returns>
        public static IEnumerable<IParameter> Flatten(this IEnumerable<IParameter> parameters)
        {
            parameters.ThrowIfNull();
            var afterFirstHost = false;
            foreach (var parameter in parameters)
            {
                if (parameter.Argument is IEnumerable<IParameter> includeParams)
                {
                    afterFirstHost = true;
                    if (parameter is IParameter<HostNode> hostParam)
                        yield return new Parameter<HostNode>(
                            hostParam.Keyword,
                            (HostNode) hostParam.Argument.Copy(),
                            hostParam.ParameterAppearance);
                    else if (parameter is IParameter<MatchNode> matchParam)
                        yield return new Parameter<MatchNode>(
                            matchParam.Keyword,
                            (MatchNode) matchParam.Argument.Copy(),
                            matchParam.ParameterAppearance);
                    
                    foreach (var includeParameters in includeParams.Flatten().ToList())
                        yield return includeParameters;
                }
                else
                {
                    if (!afterFirstHost)
                       yield return parameter;
                }
            }
        }

        /// <summary>
        /// Collects all parameters and groups them in hosts
        /// </summary>
        /// <param name="parameters">A sequence of parameters to be collected from</param>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are the result of collecting</returns>
        public static IEnumerable<IParameter> Collect(this IEnumerable<IParameter> parameters)
        {
            parameters.ThrowIfNull();
            IArgParameter<ParameterParent> lastNode = null;
            foreach (var parameter in parameters)
            {
                if (parameter.IsNode() && parameter is IArgParameter<ParameterParent> parent)
                {
                    if (lastNode != null)
                        yield return lastNode;
                    lastNode = parent;
                }
                else
                {
                    if (lastNode != null) 
                        lastNode.Argument.Add(parameter);
                    else
                        yield return parameter;
                }
            }
            if (lastNode != null)
                yield return lastNode;
        }

        /// <summary>
        /// Clones all parameters of given <paramref name="parameters"/>
        /// </summary>
        /// <param name="parameters">A sequence of parameters to invoke the clone on.</param>
        /// <typeparam name="TParam">The type of the parameters of <paramref name="parameters" /></typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are the result of invoking the clone function
        /// on each parameter of <paramref name="parameters" /></returns>
        public static IEnumerable<TParam> Cloned<TParam>(this IEnumerable<TParam> parameters)
            where TParam : IParameter =>
            parameters.Select(parameter => (TParam) parameter.Clone());

        /// <summary>
        /// Compiles the given sequence by applying
        /// <list type="bullet">
        ///<item><see cref="Flatten"/></item>
        ///<item><see cref="Collect"/></item>
        ///<item><see cref="Cloned{TParam}"/></item>
        /// </list>
        /// </summary>
        /// <param name="parameters">A sequence of parameters to be compiled</param>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are the result of the compilation</returns>
        public static IEnumerable<IParameter> Compiled(this IEnumerable<IParameter> parameters) =>
            parameters.Flatten()
                .Collect()
                .Cloned();
        
        /// <summary>
        /// Scans every parameter in the given sequence and only returns matching ones
        /// Normal parameters are collected and all <see cref="Node"/>s, that apply to <see cref="Node.Matches"/>
        /// </summary>
        /// <param name="parameters">A sequence of parameters to be matched</param>
        /// <param name="name">The name to be matched against</param>
        /// <param name="options">The options for the matching</param>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are matching the <paramref name="name"/></returns>
        public static IEnumerable<IParameter> Matching(
            this IEnumerable<IParameter> parameters,
            string name,
            MatchingOptions options = MatchingOptions.MATCHING)
        {
            parameters.ThrowIfNull();
            name.ThrowIfNull();
            options.ThrowIfNull();
            var context = new MatchingContext(name);
            foreach (var parameter in parameters)
            {
                if (parameter.Argument is Node node)
                {
                    if (node.Matches(name, context, options))
                        yield return parameter;
                }
                else
                {
                    context.SetProperty(parameter.Argument, parameter.Keyword.Name);
                    yield return parameter;
                }
            }
        }

        /// <summary>
        /// Filters the given sequence, where the parameter type is exactly of <typeparamref name="TParam"/>
        /// </summary>
        /// <param name="parameters">A sequence of parameters to be filtered</param>
        /// <param name="keyword">The <see cref="Keyword{TParam}"/> to be searched for</param>
        /// <typeparam name="TParam">The type to be filtered for</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are of type <typeparamref name="TParam"/></returns>
        public static IEnumerable<IParameter<TParam>> WhereParam<TParam>(
            this IEnumerable<IParameter> parameters,
            Keyword<TParam> keyword)
        {
            parameters.ThrowIfNull();
            keyword.ThrowIfNull();
            foreach (var item in parameters)
                    if (item.Keyword == keyword && item is IParameter<TParam> param)
                        yield return param;
            
        }
        
        /// <summary>
        /// Filters the given sequence, where the argument's type is extending from <typeparamref name="TParam"/>
        /// </summary>
        /// <param name="parameters">A sequence of parameters to be filtered</param>
        /// <typeparam name="TParam">The type the arguments should extend from</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are of type <typeparamref name="TParam"/></returns>
        public static IEnumerable<IArgParameter<TParam>> WhereArg<TParam>(this IEnumerable<IParameter> parameters)
        {
            parameters.ThrowIfNull();
            foreach (var item in parameters)
                if (item is IArgParameter<TParam> param)
                    yield return param;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters">A sequence of parameters to be selected</param>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are the selected arguments</returns>
        public static IEnumerable<object> SelectArg(this IEnumerable<IParameter> parameters) => 
            parameters.Select(p => p.Argument);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters">A sequence of parameters to be selected</param>
        /// <typeparam name="TParam">The type of arguments to be selected</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are the selected arguments
        /// of type <typeparamref name="TParam"/></returns>
        public static IEnumerable<TParam> SelectArg<TParam>(this IEnumerable<IArgParameter<TParam>> parameters) =>
            parameters.Select(p => p.Argument);

        //-----------------------------------------------------------------------//
        //                           Consumer functions
        //-----------------------------------------------------------------------//

        /// <summary>
        /// Consumes given enumerable of parameters and returns a new <see cref="SshConfig"/>
        /// </summary>
        /// <param name="parameters">A sequence of parameters to create the config of</param>
        /// <param name="fileName">Optional file name, as initializer for the SshConfig</param>
        /// <returns>SshConfig</returns>
        public static SshConfig ToConfig(this IEnumerable<IParameter> parameters, string fileName = null)
        {
            parameters.ThrowIfNull();
            return new SshConfig(fileName, parameters.Collect().ToList());
        }

        /// <summary>
        /// Consumes given enumerable and returns first free parameters as a new <see cref="HostNode"/>.
        /// If a parameter is defined multiple times (except from those intended) only the first will be there.
        /// Comments of all parents will be added if they contain anything
        /// </summary>
        /// <param name="parameters">A sequence of parameters to create the host of</param>
        /// <param name="hostName">HostName of the Host, used as initializer</param>
        /// <returns>HostNode</returns>
        public static HostNode FirstToHost(this IEnumerable<IParameter> parameters, string hostName)
        {
            parameters.ThrowIfNull();
            hostName.ThrowIfNull();
            var host = new HostNode(hostName);
            if (parameters is ParameterParent parent)
                foreach (var parentComment in parent.Comments)
                    if (!string.IsNullOrWhiteSpace(parentComment)) 
                        host.Comments.Add(parentComment);

            foreach (var param in parameters)
            {
                if (param.IsNode())
                    break;
                if (param.Keyword.AllowMultiple || !host.Has(param.Keyword))
                    host.Add(param);
            }
            return host;
        }
        
        /// <summary>
        /// Consumes the given sequence of parameters and serializes them into a string.
        /// By changing the options one can specify the look
        /// </summary>
        /// <param name="parameters">A sequence of parameters to be serialized</param>
        /// <param name="options">The options for exporting</param>
        /// <returns>Serialized string</returns>
        public static string Serialize(
            this IEnumerable<IParameter> parameters,
            SerializeConfigOptions options = SerializeConfigOptions.DEFAULT)
        {
            parameters.ThrowIfNull();
            if (parameters is IConfigSerializable serializable)
                return serializable.Serialize(options);
            var lines = new List<string>();
            lines.AddRange(parameters.Select(p => p.Serialize(options)));
            return string.Join(Environment.NewLine, lines);
        }
        
        /// <summary>
        /// Writes a file to the path specified with <paramref name="filename"/>
        /// </summary>
        /// <param name="parameters">A sequence of parameters to be written</param>
        /// <param name="filename">The path to the file</param>
        /// <returns>Result if export was successful</returns>
        public static Result WriteFile(this IEnumerable<IParameter> parameters, string filename)
        {
            parameters.ThrowIfNull();
            filename.ThrowIfNull();
            return Result.Try(() => 
                File.WriteAllText(filename, parameters.Serialize()));
        }

    }
}