using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;
using Newtonsoft.Json;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;

public class QmHttpClient : HttpClient
{
    private const string DefaultContentType = "application/json";

    public async Task<QmResponse<T>> GetAsync<T>(QmRequest qmRequest)
    {
        HttpResponseMessage httpResponse = await GetAsync(qmRequest.Url);
        return new QmResponse<T>(qmRequest, httpResponse);
    }

    /// <summary>
    /// This method can be used to explicitly set the Response.Data value upon successful or failed API responses.
    /// </summary>
    public async Task<QmResponse<T>> GetAsync<T>(QmRequest qmRequest, T successDataValue, T failedDataValue)
    {
        try
        {
            QmResponse<T> response = await GetAsync<T>(qmRequest);
            response.Data = response.IsSuccess ? successDataValue : failedDataValue;

            return response;
        }
        catch (Exception e)
        {
            Logger.Log(e.ToString());
            return new QmResponse<T>(qmRequest, new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = e.Message });
        }
    }

    public async Task<QmResponse<T>> GetAsync<T>(string url)
        => await GetAsync<T>(new QmRequest { Url = url });

    public async Task<QmResponse<T>> GetAsync<T>(string url, T successDataValue, T failedDataValue)
        => await GetAsync(new QmRequest { Url = url }, successDataValue, failedDataValue);

    public async Task<QmResponse<T>> PostAsync<T>(QmRequest qmRequest, object data)
    {
        try
        {
            HttpContent content = data as HttpContent ?? new StringContent(JsonConvert.SerializeObject(data), Encoding.Default, DefaultContentType);
            return new QmResponse<T>(qmRequest, await PostAsync(qmRequest.Url, content));
        }
        catch (Exception e)
        {
            Logger.Log(e.ToString());
            return new QmResponse<T>(qmRequest, new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = e.Message });
        }
    }

    public async Task<QmResponse<T>> PostAsync<T>(string url, object data) => await PostAsync<T>(new QmRequest { Url = url }, data);
}