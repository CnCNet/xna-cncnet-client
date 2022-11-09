using System;
using System.IO;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DXMainClient.Tests
{
    [TestFixture]
    public class QmRequestResponseConverterTests
    {
        [Test]
        public void Test_SpawnRead()
        {
            string json = GetSpawnJson();

            QmRequestResponse response = JsonConvert.DeserializeObject<QmRequestResponse>(json);

            Assert.IsInstanceOf<QmSpawnResponse>(response);
        }

        [Test]
        public void Test_SpawnWrite()
        {
            string jsonIn = GetSpawnJson();

            QmRequestResponse response = JsonConvert.DeserializeObject<QmRequestResponse>(jsonIn);

            string jsonOut = JsonConvert.SerializeObject(response);

            Console.WriteLine(jsonOut);
        }

        [Test]
        public void Test_PleaseWaitRead()
        {
            string json = GetPleaseWaitJson();

            QmRequestResponse response = JsonConvert.DeserializeObject<QmRequestResponse>(json);

            Assert.IsInstanceOf<QmWaitResponse>(response);
        }

        private static string GetSpawnJson() => GetJson("qm_spawn_response");

        private static string GetPleaseWaitJson() => GetJson("qm_please_wait_response");

        private static string GetJson(string filename) => File.ReadAllText($"TestData/QmResponses/{filename}.json");
    }
}