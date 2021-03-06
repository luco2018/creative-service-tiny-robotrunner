﻿#if NET_4_6
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny.Test
{
    /// <summary>
    /// These tests are to ensure that UnityEngine.Object value are preserved when passing through the serializaton pipeline
    /// </summary>
    [TestFixture]
    public class UnityObjectSerializationTest
    {
        private IRegistry m_Registry;
        private UTinyType m_ComponentType;
        private UTinyEntity m_Entity;
        private Texture2D m_Texture2D;

        [SetUp]
        public void SetUp()
        {
            m_Registry = new UTinyRegistry();
            
            // Create a component with a single Texture2D field
            m_ComponentType = m_Registry.CreateType(
                UTinyId.New(),
                "TestComponent",
                UTinyTypeCode.Component);

            m_ComponentType.CreateField(
                "TestTexture2DField",
                (UTinyType.Reference) UTinyType.Texture2DEntity);

            m_Entity = m_Registry.CreateEntity(UTinyId.New(), "TestEntity");
            var component = m_Entity.AddComponent((UTinyType.Reference) m_ComponentType);
            component.Refresh();
            
            // Create some asset on disc
            File.WriteAllBytes(Application.dataPath + "/TestTexture.png", new Texture2D(32, 32).EncodeToPNG());
            AssetDatabase.ImportAsset("Assets/TestTexture.png");
            m_Texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TestTexture.png");
        }
        
        [TearDown]
        public void TearDown()
        {
            var path = AssetDatabase.GetAssetPath(m_Texture2D);
            AssetDatabase.DeleteAsset(path);
        }

        /// <summary>
        /// Ensure our default values pass basic tests
        /// @NOTE This is covered more thoroughly in "DefaultValueTest.cs"
        /// </summary>
        [Test]
        public void DefaultValueSanityCheck()
        {
            var component = m_Entity.GetComponent((UTinyType.Reference) m_ComponentType);
            var value = component["TestTexture2DField"];
            Assert.AreEqual(null, value);
        }
        
        /// <summary>
        /// Ensures UnityEngine.Object values can be serialized correctly
        /// </summary>
        [Test]
        public void BinaryObjectValue()
        {
            // Output registry
            var registry = new UTinyRegistry();
            
            var component = m_Entity.GetComponent((UTinyType.Reference) m_ComponentType);
            component["TestTexture2DField"] = m_Texture2D;
            Assert.AreEqual(m_Texture2D, component["TestTexture2DField"]);

            using (var binary = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write our data to binary
                Serialization.Binary.BackEnd.Persist(binary, m_ComponentType, m_Entity);

                binary.Position = 0;

                // Read from binary to command stream
                Serialization.Binary.FrontEnd.Accept(binary, command);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }
            
            AssertObjectValue(registry);
        }
        
        /// <summary>
        /// Ensures UnityEngine.Object values can be serialized correctly
        /// </summary>
        [Test]
        public void FlatJsonObjectValue()
        {
            var component = m_Entity.GetComponent((UTinyType.Reference) m_ComponentType);
            component["TestTexture2DField"] = null;
            component["TestTexture2DField"] = m_Texture2D;
            Assert.AreEqual(m_Texture2D, component["TestTexture2DField"]);

            // Output registry
            var registry = new UTinyRegistry();
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                Serialization.FlatJson.BackEnd.Persist(json, m_ComponentType, m_Entity);

                json.Position = 0;

                var reader = new StreamReader(json);
                Debug.Log(reader.ReadToEnd());

                json.Position = 0;

                Serialization.FlatJson.FrontEnd.Accept(json, command);

                command.Position = 0;

                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }
            
            AssertObjectValue(registry);
        }

        private void AssertObjectValue(IRegistry registry)
        {
            var componentType = registry.FindById<UTinyType>(m_ComponentType.Id);
            var entity = registry.FindById<UTinyEntity>(m_Entity.Id);
            var component = entity.GetComponent((UTinyType.Reference) componentType);
            Assert.AreEqual(m_Texture2D, component["TestTexture2DField"]);
        }
    }
}
#endif // NET_4_6
