﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using SshTools.Line.Parameter.Keyword;
using SshTools.Parent.Host;
using SshTools.Parent.Match;
using Xunit;
using static SshTools.Tests.Unit.ConfigResources;

namespace SshTools.Tests.Unit.Parent
{
    public class ParameterParentTests
    {
        public static IEnumerable<object[]> GetDataTypes = new[]
        {
            new object[]{ Keyword.Host, typeof(HostNode) },
            new object[]{ Keyword.Match, typeof(MatchNode) },
            new object[]{ Keyword.HostName, typeof(string) },
            new object[]{ Keyword.IdentitiesOnly, typeof(bool) },
            new object[]{ Keyword.IdentityFile, typeof(string) },
            new object[]{ Keyword.Port, typeof(ushort) },
            new object[]{ Keyword.User, typeof(string) }
        };
        
        [Theory]
        [MemberData(nameof(GetDataTypes))]
        public void TestDataTypes(Keyword keyword, Type type)
        {
            var config = DeserializeString(ConfigWithEveryParameter);

            config[keyword].Should().BeOfType(type);
        }
    }
}