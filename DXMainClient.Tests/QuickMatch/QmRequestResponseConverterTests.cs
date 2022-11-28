using System;
using System.IO;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DXMainClient.Tests.QuickMatch
{
    [TestFixture]
    public class QmRequestResponseConverterTests
    {
        [Test]
        public void Test_SpawnRead()
        {
            string json = GetSpawnJson();

            QmSpawnResponse response = JsonConvert.DeserializeObject<QmSpawnResponse>(json);

            Assert.IsInstanceOf<QmSpawnResponse>(response);
        }

        [Test]
        public void Test_SpawnWrite()
        {
            string jsonIn = GetSpawnJson();

            QmSpawnResponse response = JsonConvert.DeserializeObject<QmSpawnResponse>(jsonIn);

            string jsonOut = JsonConvert.SerializeObject(response);

            Console.WriteLine(jsonOut);
        }

        [Test]
        public void Test_PleaseWaitRead()
        {
            string json = GetPleaseWaitJson();

            QmWaitResponse response = JsonConvert.DeserializeObject<QmWaitResponse>(json);

            Assert.IsInstanceOf<QmWaitResponse>(response);
        }

        private static string GetSpawnJson() => GetJson("qm_spawn_response");

        private static string GetPleaseWaitJson() => GetJson("qm_please_wait_response");

        private static string GetJson(string filename) => File.ReadAllText($"TestData/QuickMatch/QmResponses/{filename}.json");
    }
}