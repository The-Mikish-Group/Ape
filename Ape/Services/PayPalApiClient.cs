using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ape.Models.PayPal;

namespace Ape.Services;

public class PayPalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayPalApiClient> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _baseUrl;

    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly object _tokenLock = new();

    public PayPalApiClient(string clientId, string clientSecret, string mode, ILogger<PayPalApiClient> logger)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _logger = logger;
        _baseUrl = mode?.Equals("live", StringComparison.OrdinalIgnoreCase) == true
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";
        _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
    }

    private async Task EnsureAccessTokenAsync()
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
            return;

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("grant_type", "client_credentials")])
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<PayPalTokenResponse>(json);

        lock (_tokenLock)
        {
            _accessToken = token?.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds((token?.ExpiresIn ?? 3600) - 300); // 5 min buffer
        }
    }

    public async Task<PayPalOrderResponse> CreateOrderAsync(PayPalOrderRequest orderRequest)
    {
        await EnsureAccessTokenAsync();

        var json = JsonSerializer.Serialize(orderRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, "/v2/checkout/orders")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal CreateOrder failed: {Status} {Response}", response.StatusCode, responseJson);
            throw new HttpRequestException($"PayPal CreateOrder failed: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<PayPalOrderResponse>(responseJson) ?? new();
    }

    public async Task<PayPalCaptureResponse> CaptureOrderAsync(string orderId)
    {
        await EnsureAccessTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/v2/checkout/orders/{orderId}/capture")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal CaptureOrder failed: {Status} {Response}", response.StatusCode, responseJson);
            throw new HttpRequestException($"PayPal CaptureOrder failed: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<PayPalCaptureResponse>(responseJson) ?? new();
    }

    public async Task<PayPalRefundResponse> RefundCaptureAsync(string captureId, decimal? amount = null)
    {
        await EnsureAccessTokenAsync();

        string body = "{}";
        if (amount.HasValue)
        {
            body = JsonSerializer.Serialize(new { amount = new { value = amount.Value.ToString("F2"), currency_code = "USD" } });
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"/v2/payments/captures/{captureId}/refund")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal RefundCapture failed: {Status} {Response}", response.StatusCode, responseJson);
            throw new HttpRequestException($"PayPal RefundCapture failed: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<PayPalRefundResponse>(responseJson) ?? new();
    }

    public async Task<PayPalSubscriptionResponse> CreateSubscriptionAsync(PayPalSubscriptionRequest subscriptionRequest)
    {
        await EnsureAccessTokenAsync();

        var json = JsonSerializer.Serialize(subscriptionRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/billing/subscriptions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal CreateSubscription failed: {Status} {Response}", response.StatusCode, responseJson);
            throw new HttpRequestException($"PayPal CreateSubscription failed: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<PayPalSubscriptionResponse>(responseJson) ?? new();
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, string reason)
    {
        await EnsureAccessTokenAsync();

        var json = JsonSerializer.Serialize(new { reason });
        var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/billing/subscriptions/{subscriptionId}/cancel")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogError("PayPal CancelSubscription failed: {Status} {Response}", response.StatusCode, responseJson);
        }
    }
}
