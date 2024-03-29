﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SshTools.Line;
using SshTools.Line.Parameter;
using SshTools.Line.Parameter.Keyword;
using SshTools.Serialization;

namespace SshTools.Parent
{
    public abstract class ParameterParent : IList<ILine>, IConfigSerializable, ICloneable
    {
        //-----------------------------------------------------------------------//
        //                   ParameterParent Keyword Properties
        //-----------------------------------------------------------------------//
        
        public string HostName { get => this.Get(Keyword.HostName); set => this.Set(Keyword.HostName, value); }
        public bool IdentitiesOnly { get => this.Get(Keyword.IdentitiesOnly); set => this.Set(Keyword.IdentitiesOnly, value); }
        public string IdentityFile { get => this.Get(Keyword.IdentityFile); set => this.Set(Keyword.IdentityFile, value); }
        public ushort Port { get => this.Get(Keyword.Port); set => this.Set(Keyword.Port, value); }
        public string User { get => this.Get(Keyword.User); set => this.Set(Keyword.User, value); }

        //-----------------------------------------------------------------------//
        //                          ParameterParent Logic
        //-----------------------------------------------------------------------//
        
        private IList<ILine> Params { get; }

        internal ParameterParent(IList<ILine> parameters = null) => 
            Params = parameters ?? new List<ILine>();

        
        public object this[Keyword keyword] => this.Get(keyword);
        public virtual string Serialize(SerializeConfigOptions options = SerializeConfigOptions.DEFAULT) => 
            string.Join(Environment.NewLine, this.Select(p => p.Serialize(options)));
        public abstract object Clone();

        
        //-----------------------------------------------------------------------//
        //                          IList Implementation
        //-----------------------------------------------------------------------//
        
        public int Count => Params.Count;
        public bool IsReadOnly => false;
        
        public IEnumerator<ILine> GetEnumerator() => Params.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void IfValidItem(ILine item, Action action)
        {
            if (item == null) return;
            if (this is Node && item is IParameter param && param.Keyword.IsNode())
                throw new Exception($"Invalid keyword {param.Keyword} cannot be added to parent {GetType().Name}!");
            action();
        }
        public void Add(ILine item) =>
            IfValidItem(item, () => Params.Add(item));

        public void Insert(int index, ILine item) => 
            IfValidItem(item, () => Params.Insert(index, item));
        public void Clear() => Params.Clear();
        public bool Contains(ILine item) => Params.Contains(item);
        public void CopyTo(ILine[] array, int arrayIndex) => Params.CopyTo(array, arrayIndex);
        public bool Remove(ILine item) => Params.Remove(item);
        public int IndexOf(ILine item) => Params.IndexOf(item);
        public void RemoveAt(int index) => Params.RemoveAt(index);
        public ILine this[int index]
        {
            get => Params[index];
            set => Params[index] = value;
        }
    }
}