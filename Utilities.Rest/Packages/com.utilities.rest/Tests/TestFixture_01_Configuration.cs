// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System.IO;
using UnityEngine;

namespace Utilities.WebRequestRest.Tests
{
    public class TestFixture_01_Configuration
    {
        [Test]
        public void TestFixture_01_DownloadCache_Locations()
        {
            Debug.Log(Rest.DownloadLocation);
            Debug.Log(Rest.DownloadCacheDirectory);
            Assert.AreEqual(Application.temporaryCachePath, Rest.DownloadLocation);
            Assert.IsTrue(Directory.Exists(Rest.DownloadCacheDirectory));

            Rest.DownloadLocation = Application.persistentDataPath;
            Debug.Log(Rest.DownloadLocation);
            Debug.Log(Rest.DownloadCacheDirectory);
            Assert.AreEqual(Application.persistentDataPath, Rest.DownloadLocation);
            Assert.IsTrue(Directory.Exists(Rest.DownloadCacheDirectory));

            Rest.DownloadLocation = Application.dataPath;
            Debug.Log(Rest.DownloadLocation);
            Debug.Log(Rest.DownloadCacheDirectory);
            Assert.AreEqual(Application.dataPath, Rest.DownloadLocation);
            Assert.IsTrue(Directory.Exists(Rest.DownloadCacheDirectory));

            Rest.DownloadLocation = Application.streamingAssetsPath;
            Debug.Log(Rest.DownloadLocation);
            Debug.Log(Rest.DownloadCacheDirectory);
            Assert.AreEqual(Application.streamingAssetsPath, Rest.DownloadLocation);
            Assert.IsTrue(Directory.Exists(Rest.DownloadCacheDirectory));
        }
    }
}
