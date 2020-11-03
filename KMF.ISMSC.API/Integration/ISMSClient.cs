using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace KMF.ISMSC.API.Integration
{
    public class ISMSClient : HttpClient
    {
        private HttpClient _client;
        private static JsonSerializerOptions _jsonOptions;
        private static JsonSerializerOptions JsonOptions = _jsonOptions ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        public ISMSClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<object> Send(Message message, CancellationToken ct) 
        {

            var body = JsonSerializer.Serialize(message, JsonOptions);
            var stringContent = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("http://isms.center/api/sms/send", stringContent, ct);

            switch (response.StatusCode) 
            {
                case HttpStatusCode.OK:
                    await using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        var content = await JsonSerializer.DeserializeAsync<SuccessResponse>(stream, JsonOptions, ct);
                        return content;
                    }
                case HttpStatusCode.BadRequest: 
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.NotFound:
                    await using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        var content = await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, JsonOptions, ct);
                        return content;
                    }
                default:
                    throw new Exception(response.StatusCode.ToString());
            } 


           
        }

        public async Task<object> SendMulticast(IEnumerable<Message> messages, CancellationToken ct) 
        {

            var body = JsonSerializer.Serialize(messages, JsonOptions);
            var stringContent = new StringContent(body, Encoding.UTF8, "application/json");


            var response = await _client.PostAsync("http://isms.center/api/sms/send", stringContent, ct);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    await using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        var content = await JsonSerializer.DeserializeAsync<SuccessResponse>(stream, JsonOptions, ct);
                        return content;
                    }
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.NotFound:
                    await using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        var content = await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, JsonOptions, ct);
                        return content;
                    }
                default:
                    throw new Exception(response.StatusCode.ToString());
            }

        }

        

        private class Message
        {

            [JsonPropertyName("message_id")]
            public string MessageId { get; set; }
            public string To { get; set; }
            [JsonPropertyName("sms_count")]
            public string SmsCount { get; set; }
        }

        private class SuccessResponse 
        {

            [JsonPropertyName("message_id")]
            public string MessageId { get; set; }

            public string To { get; set; }
            public string Status { get; set; }
            [JsonPropertyName("sms_count")]
            public string SmsCount { get; set; }
        }

        private class ErrorResponse
        {
            [JsonPropertyName("error_code")]
            public int ErrorCode { get; set; }

            [JsonPropertyName("error_message")]
            public string ErrorMessage { get; set; }        
        }

        private class ReportResponse
        {
            [JsonPropertyName("bulk_id")]
            public string BuilId { get; set; }
            [JsonPropertyName("message_id")]
            public string MessageId { get; set; }
            public string To { get; set; }
            [JsonPropertyName("sent_at")]
            public DateTime? SentAt { get; set; }
            [JsonPropertyName("done_at")]
            public DateTime? DoneAt { get; set; }
            [JsonPropertyName("sms_count")]
            public string SmsCount { get; set; }

            [JsonPropertyName("callback_data")]
            public string CallbackData { get; set; }
            public string Status { get; set; } //Статус состояния (send, sending, sent, delivered, undelivered)
            public string Err { get; set; }
        }
    }
}
