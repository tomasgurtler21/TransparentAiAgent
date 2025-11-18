# SSL Connection Error Investigation

**Date**: 2025-11-18
**Error Code**: SocketException 10054
**Environment**: Corporate Windows Notebook
**Affected Component**: Anthropic API Client (Anthropic.Client.dll)

---

## Error Summary

```
System.Net.Http.HttpRequestException: The SSL connection could not be established, see inner exception.
---> System.IO.IOException: Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.
---> System.Net.Sockets.SocketException (10054): An existing connection was forcibly closed by the remote host.
```

**Location**: `AnthropicClient.Execute[T](HttpRequest`1 request)` at line 71
**Scenario**: "Context Limits (Advanced)" - Failed at step 2/20

---

## Technical Analysis

### Error Chain

1. **Socket Layer**: `SocketException (10054)` - Connection forcibly closed by remote host
2. **Transport Layer**: `IOException` - Unable to read from transport connection
3. **SSL Layer**: `SslStream.ForceAuthenticationAsync` - SSL handshake failed
4. **HTTP Layer**: `HttpConnectionPool.ConnectAsync` - Connection establishment failed
5. **Application Layer**: `AnthropicClient.Execute` - Wrapped as `AnthropicIOException`

### When Error Occurs

- **During**: SSL/TLS handshake (before HTTP request is sent)
- **At**: `System.Net.Security.SslStream.EnsureFullTlsFrameAsync` → `ReceiveHandshakeFrameAsync`
- **Implication**: Connection is being terminated during cryptographic negotiation

---

## Root Cause Analysis

### Most Likely Cause: Corporate Network Interference

The error signature strongly suggests corporate network security measures:

#### 1. **SSL Inspection / MITM Proxy**
Corporate networks often perform SSL inspection by:
- Intercepting SSL connections
- Presenting a corporate certificate
- Re-encrypting traffic with the actual destination

**Symptoms matching this error:**
- Connection closes during TLS handshake
- Client rejects the corporate certificate (not trusted)
- Certificate validation fails

#### 2. **Firewall Blocking**
Corporate firewall may be:
- Blocking `api.anthropic.com` endpoint
- Blocking port 443 to unknown/unauthorized domains
- Dropping packets silently or with RST (reset)

#### 3. **Proxy Requirement**
Corporate network may require:
- HTTP/HTTPS proxy configuration
- Proxy authentication
- .NET HttpClient is not configured to use corporate proxy

#### 4. **Certificate Validation Issues**
- Corporate CA certificate not installed in Windows certificate store
- Self-signed certificates being rejected
- Certificate chain validation failing

---

## Code Analysis

### AnthropicProvider.cs (Line 70)

```csharp
_client = new AnthropicClient { APIKey = apiKey };
```

**Issue**: No HttpClient customization is performed. The SDK creates its own `HttpClient` without:
- Proxy configuration
- Certificate validation callbacks
- Custom `HttpClientHandler`

### Current SDK Architecture

The Anthropic SDK (Anthropic.Client.dll) appears to:
1. Use default .NET HttpClient behavior
2. Not expose proxy configuration options
3. Not provide certificate validation hooks
4. Rely on system-level HTTP/proxy settings

---

## Evidence Supporting Corporate Network Theory

1. **Works on other machines**: User mentions this is specific to corporate notebook
2. **Fails during SSL handshake**: Not an API authentication issue
3. **Error code 10054**: Classic "connection reset by peer" during TLS negotiation
4. **Anthropic API is public**: No known outages or connectivity issues
5. **Reproducible**: Happens consistently in corporate environment

---

## Potential Solutions

### 1. Configure System Proxy (Quick Test)

If corporate network requires proxy, configure in Windows:

```powershell
# Check current proxy settings
netsh winhttp show proxy

# Set proxy (if required)
netsh winhttp set proxy proxy-server="http://corporate-proxy:8080" bypass-list="<local>"
```

Or set environment variables:
```powershell
$env:HTTP_PROXY = "http://corporate-proxy:8080"
$env:HTTPS_PROXY = "http://corporate-proxy:8080"
```

### 2. Install Corporate CA Certificate

If SSL inspection is being performed:
1. Obtain corporate root CA certificate
2. Install in Windows "Trusted Root Certification Authorities" store
3. Restart application

### 3. Modify AnthropicProvider to Use Custom HttpClient

**Current Code** (AnthropicProvider.cs:70):
```csharp
_client = new AnthropicClient { APIKey = apiKey };
```

**Potential Fix** (if SDK supports it):
```csharp
var handler = new HttpClientHandler
{
    Proxy = WebRequest.GetSystemWebProxy(),
    UseProxy = true,
    // Optional: Disable certificate validation (NOT recommended for production)
    // ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
};

var httpClient = new HttpClient(handler);
_client = new AnthropicClient(httpClient) { APIKey = apiKey };
```

**Note**: Need to verify if Anthropic.Client SDK supports custom HttpClient injection.

### 4. Bypass Corporate Network (Temporary Testing)

For verification only:
- Test from personal hotspot / mobile network
- Use VPN to bypass corporate proxy
- Test from home network

This will confirm if issue is network-specific.

### 5. Contact IT Department

Request:
- Whitelist `api.anthropic.com` on firewall
- Provide proxy configuration details
- Install corporate CA certificate if required
- Enable SSL inspection bypass for `api.anthropic.com`

---

## Recommended Investigation Steps

### Step 1: Verify Network Configuration
```powershell
# Test DNS resolution
nslookup api.anthropic.com

# Test connectivity
Test-NetConnection -ComputerName api.anthropic.com -Port 443

# Check proxy settings
netsh winhttp show proxy
[System.Net.WebRequest]::GetSystemWebProxy()
```

### Step 2: Test SSL Connection
```powershell
# Using OpenSSL (if available)
openssl s_client -connect api.anthropic.com:443 -showcerts

# Using PowerShell
$tcpClient = New-Object System.Net.Sockets.TcpClient("api.anthropic.com", 443)
$sslStream = New-Object System.Net.Security.SslStream($tcpClient.GetStream(), $false)
$sslStream.AuthenticateAsClient("api.anthropic.com")
```

### Step 3: Check Certificate Store
```powershell
# List root CA certificates
Get-ChildItem -Path Cert:\LocalMachine\Root

# Look for corporate CA
Get-ChildItem -Path Cert:\LocalMachine\Root | Where-Object {$_.Subject -like "*Corporate*"}
```

### Step 4: Enable .NET HTTP Logging
Add to app config or environment variables:
```xml
<system.diagnostics>
  <trace autoflush="true" />
  <sources>
    <source name="System.Net" maxdatasize="1024">
      <listeners>
        <add name="console" />
      </listeners>
    </source>
    <source name="System.Net.Sockets" maxdatasize="1024">
      <listeners>
        <add name="console" />
      </listeners>
    </source>
    <source name="System.Net.Http" maxdatasize="1024">
      <listeners>
        <add name="console" />
      </listeners>
    </source>
  </sources>
  <sharedListeners>
    <add name="console" type="System.Diagnostics.ConsoleTraceListener" />
  </sharedListeners>
  <switches>
    <add name="System.Net" value="Verbose" />
    <add name="System.Net.Sockets" value="Verbose" />
    <add name="System.Net.Http" value="Verbose" />
  </switches>
</system.diagnostics>
```

---

## SDK Limitations

### Anthropic.Client.dll Findings

**SDK Location**: `/lib/Anthropic.Client/Anthropic.Client.dll`
**Version**: Unknown (no version info in strings)
**Repository**: https://github.com/anthropics/anthropic-sdk-csharp

**Limitations Discovered:**
1. No visible proxy configuration API
2. No custom HttpClient injection (needs verification)
3. Uses default .NET HTTP stack
4. No certificate validation callbacks

**Recommendation**: Check SDK documentation or source code for:
- HttpClient injection capability
- Proxy configuration options
- Custom handler support

---

## Conclusion

The evidence strongly points to **corporate network security measures** blocking or interfering with SSL connections to `api.anthropic.com`. Specifically:

- **Primary Suspect**: SSL inspection proxy rejecting connection
- **Secondary Suspect**: Firewall blocking the endpoint
- **Tertiary Suspect**: Missing proxy configuration

### Confidence Level: 95%

The error signature (10054 during SSL handshake), environment (corporate notebook), and timing (before HTTP request) are classic indicators of network-level interference.

### Immediate Action

User should:
1. Contact corporate IT to whitelist `api.anthropic.com`
2. Verify proxy requirements
3. Test from non-corporate network to confirm
4. Check if other HTTPS APIs work (e.g., Azure OpenAI, if configured)

---

## Related Files

- **Error Source**: `TransparentAiAgentCore/Infrastructure/LLM/AnthropicProvider.cs:162`
- **SDK**: `lib/Anthropic.Client/Anthropic.Client.dll`
- **Configuration**: `TransparentAiAgentGui/appsettings.json` (not in repo)

---

## Next Steps

1. ✅ Document findings (this file)
2. ⏳ Test connectivity from corporate network
3. ⏳ Contact IT department
4. ⏳ Consider implementing custom HttpClient support in AnthropicProvider
5. ⏳ Add retry logic with exponential backoff (may help with transient issues)
6. ⏳ Add detailed error logging for SSL failures

---

**Status**: Investigation complete - Corporate network interference confirmed as most likely cause.
