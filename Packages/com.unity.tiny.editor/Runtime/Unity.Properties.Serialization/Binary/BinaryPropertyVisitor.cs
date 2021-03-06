﻿#if NET_4_6
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Assertions;

namespace Unity.Properties.Serialization
{
    public static class BinaryToken
    {
        public const byte None = 0;
        public const byte BeginObject = 1;
        public const byte EndObject = 2;
        public const byte BeginArray = 3;
        public const byte EndArray = 4;
        public const byte Property = 5;
        public const byte Value = 6;
    }

    public class BinaryPropertyVisitor : PropertyVisitor, ICustomVisitPrimitives
    {
        private readonly Stack<long> m_PositionStack = new Stack<long>();
        
        public BinaryWriter Writer { protected get; set; }
        
        protected BinaryPropertyVisitor()
        {
        }

        public void WriteContainer<TContainer>(TContainer container) where TContainer : class, IPropertyContainer
        {
            Writer.Write(BinaryToken.BeginObject);
            Writer.Write((uint) 0);
            m_PositionStack.Push(Writer.BaseStream.Position);
            
            container.Visit(this);
            
            Writer.Write(BinaryToken.EndObject);
            PrependSize();
        }

        protected void WriteValuePropertyHeader(TypeCode typeCode)
        {
            if (!IsListItem)
            {
                Writer.Write(BinaryToken.Property);
                Writer.Write(Property.Name);
            }
            Writer.Write(BinaryToken.Value);
            Writer.Write((byte) typeCode);
        }

        protected override bool BeginContainer()
        {
            if (!IsListItem)
            {
                Writer.Write(BinaryToken.Property);
                Writer.Write(Property.Name);
            }
            Writer.Write(BinaryToken.BeginObject);
            Writer.Write((uint) 0);
            m_PositionStack.Push(Writer.BaseStream.Position);
            return true;
        }

        protected override void EndContainer()
        {
            Writer.Write(BinaryToken.EndObject);
            PrependSize();
        }

        protected override bool BeginList()
        {
            if (!IsListItem)
            {
                Writer.Write(BinaryToken.Property);
                Writer.Write(Property.Name);
            }
            Writer.Write(BinaryToken.BeginArray);
            Writer.Write((uint) 0);
            m_PositionStack.Push(Writer.BaseStream.Position);
            return true;
        }

        protected override void EndList()
        {
            Writer.Write(BinaryToken.EndArray);
            PrependSize();
        }

        private void PrependSize()
        {
            var start = m_PositionStack.Pop();
            var end = Writer.BaseStream.Position;
            var size = end - start;
            Assert.IsTrue(size <= uint.MaxValue);

            Writer.BaseStream.Position = start - sizeof(uint);
            Writer.Write((uint) size);
            Writer.BaseStream.Position = end;    
        }

        protected override void Visit<TValue>(TValue value)
        {
            if (typeof(TValue).IsEnum)
            {
                WriteValuePropertyHeader(TypeCode.Int32);
                Writer.Write(Convert.ToInt32(value));
            }
            else
            {
                throw new NotImplementedException($"Objects of type {typeof(TValue).FullName} are not supported");
            }
        }

        void ICustomVisit<sbyte>.CustomVisit(sbyte value)
        {
            WriteValuePropertyHeader(TypeCode.SByte);
            Writer.Write(value);
        }

        void ICustomVisit<short>.CustomVisit(short value)
        {
            WriteValuePropertyHeader(TypeCode.Int16);
            Writer.Write(value);
        }

        void ICustomVisit<int>.CustomVisit(int value)
        {
            WriteValuePropertyHeader(TypeCode.Int32);
            Writer.Write(value);
        }

        void ICustomVisit<long>.CustomVisit(long value)
        {
            WriteValuePropertyHeader(TypeCode.Int64);
            Writer.Write(value);
        }

        void ICustomVisit<byte>.CustomVisit(byte value)
        {
            WriteValuePropertyHeader(TypeCode.Byte);
            Writer.Write(value);
        }

        void ICustomVisit<ushort>.CustomVisit(ushort value)
        {
            WriteValuePropertyHeader(TypeCode.UInt16);
            Writer.Write(value);
        }

        void ICustomVisit<uint>.CustomVisit(uint value)
        {
            WriteValuePropertyHeader(TypeCode.UInt32);
            Writer.Write(value);
        }

        void ICustomVisit<ulong>.CustomVisit(ulong value)
        {
            WriteValuePropertyHeader(TypeCode.UInt64);
            Writer.Write(value);
        }

        void ICustomVisit<float>.CustomVisit(float value)
        {
            WriteValuePropertyHeader(TypeCode.Single);
            Writer.Write(value);
        }

        void ICustomVisit<double>.CustomVisit(double value)
        {
            WriteValuePropertyHeader(TypeCode.Double);
            Writer.Write(value);
        }

        void ICustomVisit<char>.CustomVisit(char value)
        {
            WriteValuePropertyHeader(TypeCode.Char);
            Writer.Write(value);
        }

        void ICustomVisit<string>.CustomVisit(string value)
        {
            WriteValuePropertyHeader(TypeCode.String);
            Writer.Write(value ?? string.Empty);
        }

        void ICustomVisit<bool>.CustomVisit(bool value)
        {
            WriteValuePropertyHeader(TypeCode.Boolean);
            Writer.Write(value);
        }
    }
}
#endif // NET_4_6
