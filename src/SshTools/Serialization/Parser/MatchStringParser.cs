﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentResults;
using SshTools.Parent.Match.Criteria;

namespace SshTools.Serialization.Parser
{
    public static class MatchStringParser
    {
        private static readonly Regex Regex = new Regex("[^\\s]+|[\\s]+", RegexOptions.Compiled);
        
        /// <summary>
        /// Parses the argument of a Match into a list of criteria with optionally arguments and spacing <br/>
        /// Rules:
        /// <list type="bullet">
        /// <item>Criteria are case sensitive</item>
        /// <item>Multiple criteria can be separated by ' ' or ','</item>
        /// <item>Spacing and arguments are null if not available</item>
        /// <item>Method will return failure if unknown criteria or unexpected arguments are found</item>
        /// </list>
        /// </summary>
        /// <param name="str">The match argument to be parsed</param>
        /// <returns>A list with all information</returns>
        public static Result<IList<(Criteria, string, string, string)>> Parse(string str)
        {
            if (str is null)
                return Result.Fail("Could not parse criteria. Could not parse string <null>");
            var matches = Regex.Matches(str);
            IList<(Criteria, string, string, string)> list = new List<(Criteria, string, string, string)>();

            var i = 0;
            while (i < matches.Count)
            {
                var match = matches[i];
                var val = match.Value;
                if (!SshTools.Settings.Has<Criteria>(val))
                    return Result.Fail($"Expected a criteria! '{val}' is not matching at position {i} of string '{str}'");
                
                var criteria = SshTools.Settings.Get<Criteria>(val);
                string spacing = null;
                string value = null;
                
                if (criteria is ArgumentCriteria)
                {
                    if (i + 2 >= matches.Count)
                        return Result.Fail($"Got argument criteria '{val}' at position {i} of string '{str}'," + 
                                           " but could not find an argument afterwards");
                    spacing = matches[i + 1].Value;
                    value = matches[i + 2].Value;
                    i += 2;
                }
                string spacingBack = null;
                if (matches.Count > i + 1)
                {
                    spacingBack = matches[i + 1].Value;
                    i++;
                }
                var res = criteria.Check(list.Select(c => c.Item1).ToArray());
                if (res.IsFailed)
                    return Result.Merge(
                        Result.Fail($"Criteria '{val} cannot be combined with other criteria in '{str}'"),
                        res);
                list.Add((criteria, spacing, value, spacingBack));
                i++;
            }
            return Result.Ok(list);
        }
    }
}