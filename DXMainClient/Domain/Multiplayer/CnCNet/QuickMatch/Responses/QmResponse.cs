using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Converters;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;
using Newtonsoft.Json;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;

public class QmResponse<T>
{
    public QmRequest Request { get; }

    public T Data
    {
        get => _Data ?? GetData();
        set => _Data = value;
    }

    private T _Data;

    private HttpResponseMessage Response { get; }

    public QmResponse(QmRequest request = null, HttpResponseMessage response = null)
    {
        Request = request;
        Response = response;
    }

    public bool IsSuccess => Response?.IsSuccessStatusCode ?? false;

    public string ReasonPhrase => Response?.ReasonPhrase;

    public HttpStatusCode StatusCode => Response?.StatusCode ?? HttpStatusCode.InternalServerError;

    public Task<string> ReadContentAsStringAsync() => Response?.Content.ReadAsStringAsync();

    private T GetData()
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(ReadContentAsStringAsync().Result);
        }
        catch (Exception e)
        {
            Logger.Log(e.ToString());
            return default;
        }
    }
}

public class QmResponse : QmResponse<object>
{
}