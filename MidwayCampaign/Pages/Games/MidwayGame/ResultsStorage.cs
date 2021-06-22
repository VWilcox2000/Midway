using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public partial class ResultsStorage
  {
    public static async Task StoreResults(
      string? sourceIp,
      ResultsData results)
    {
      try
      {
        CloudStorageAccount storageAccount;
        CloudBlobClient blobClient;
        CloudBlobContainer container;
        CloudBlockBlob blockBlob;
        DateTimeOffset now;
        byte[] buffer;
        string name;
        string json;

        now = DateTimeOffset.UtcNow;
        name = string.Format(
          "MidwayResults_{0}_{1}_{2}__{3}_{4}_{5}_{6}.json",
          now.Year,
          now.Month,
          now.Day,
          now.Hour,
          now.Minute,
          now.Second,
          now.Millisecond);
        storageAccount = CloudStorageAccount.Parse(
          getStorageAccountConnectionString());
        blobClient = storageAccount.CreateCloudBlobClient();
        container = blobClient.GetContainerReference(
          "midwayresults");
        await container.CreateIfNotExistsAsync();
        now = DateTimeOffset.UtcNow;
        blockBlob = container.GetBlockBlobReference(name);
        results.ip = sourceIp;
        json = Newtonsoft.Json.JsonConvert.SerializeObject(results);
        buffer = System.Text.UTF8Encoding.UTF8.GetBytes(json);
        using (MemoryStream stream = new MemoryStream(buffer))
        {
          await blockBlob.UploadFromStreamAsync(stream);
        }
      }
      catch (Exception exception)
      {
        System.Diagnostics.Trace.WriteLine(
          exception.Message +
          " @ " +
          exception.StackTrace);
        Debug.Assert(false);
      }
    }

    private static string getStorageAccountConnectionString()
    {
      PropertyInfo? pi;
      string ret;

      ret = "?";
      pi = typeof(ResultsStorage).GetProperty(
        "storageConn",
        BindingFlags.Static |
        BindingFlags.NonPublic);
      if (pi != null)
      {
        ret = (pi.GetValue(null) as string) ?? String.Empty;
      }
      return ret;
    }
  }
}
