﻿using System;
using System.Collections.Generic;
using FluentResults;
using SshTools.Config.Matching;
using SshTools.Config.Parameters;

namespace SshTools.Config.Parents
{
    // ReSharper disable UnusedMember.Global
    // ReSharper disable once UnusedType.Global
    public static class NodeExtensions
    {
        private const string HostWarning =
            "A node cannot contain a Host, therefore this method is invalid.\n" + 
            "Use this method only on SshConfigs";
        
        [Obsolete(HostWarning, true)]
        public static bool Has(this Node node, string hostName, MatchingOptions options = MatchingOptions.EXACT) =>
            throw new NotImplementedException();
        
        [Obsolete(HostWarning, true)]
        public static int IndexOf(this Node node, string hostName) =>
            throw new NotImplementedException();
        
        [Obsolete(HostWarning, true)]
        public static HostNode Get(this Node node, string hostName) =>
            throw new NotImplementedException();

        [Obsolete(HostWarning, true)]
        public static HostNode Find(this Node node, string hostName,
            MatchingOptions options = MatchingOptions.MATCHING) =>
            throw new NotImplementedException();
        
        [Obsolete(HostWarning, true)]
        public static Result<HostNode> InsertHost(this Node node, int index, string hostName) =>
            throw new NotImplementedException();
        
        [Obsolete(HostWarning, true)]
        public static Result<MatchNode> InsertMatch(this Node node, int index, Criteria criteria) =>
            throw new NotImplementedException();
        
        [Obsolete(HostWarning, true)]
        public static Result<MatchNode> InsertMatch(this Node node, int index, ArgumentCriteria criteria, string argument) =>
            throw new NotImplementedException();
        
        [Obsolete(HostWarning, true)]
        public static Result<HostNode> SetHost(this Node node, string hostName) =>
            throw new NotImplementedException();

        [Obsolete(HostWarning, true)]
        public static Result<MatchNode> SetMatch(this Node node, Criteria criteria) =>
            throw new NotImplementedException();

        [Obsolete(HostWarning, true)]
        public static Result<MatchNode> SetMatch(this Node node, ArgumentCriteria criteria, string argument) =>
            throw new NotImplementedException();

    }
}