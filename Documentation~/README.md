# com.utilities.rest

[![openupm](https://img.shields.io/npm/v/com.utilities.rest?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.utilities.rest/)

A Utilities.Rest package for the [Unity](https://unity.com/) Game Engine.

## Installing

### Via Unity Package Manager and OpenUPM

- Open your Unity project settings
- Select the `Package Manager`
![scoped-registries](images/package-manager-scopes.png)
- Add the OpenUPM package registry:
  - `Name: OpenUPM`
  - `URL: https://package.openupm.com`
  - `Scope(s):`
    - `com.utilities`
- Open the Unity Package Manager window
- Change the Registry from Unity to `My Registries`
- Add the `Utilities.Rest` package

### Via Unity Package Manager and Git url

- Open your Unity Package Manager
- Add package from git url: `https://github.com/RageAgainstThePixel/com.utilities.rest.git#upm`
  > Note: this repo has dependencies on other repositories! You are responsible for adding these on your own.
  - [com.utilities.async](https://github.com/RageAgainstThePixel/com.utilities.async)

---

## Documentation

This library aims to provide basic support for common RESTful state transactions with most web APIs.

Advanced features includes progress notifications, authentication and native multimedia downloads of asset bundles, textures, audio clips with file caching.

### Table of contents

- [Authentication](#authentication)
- [Get](#get)
- [Post](#post)
- [Put](#put)
- [Patch](#patch)
- [Delete](#delete)
- [Download Multimedia](#multimedia)
  - [Caching](#caching)
  - [Files](#files)
  - [Textures](#textures)
  - [Audio](#audio)
  - [Asset Bundles](#asset-bundles)

### Authentication

```csharp
// Get a basic auth header encoded to base64 string:
var basicAuthentication = Rest.GetBasicAuthentication("username", "password");

// Get a bearer auth token header
var bearerToken = Rest.GetBearerOAuthToken("authToken");
```

### Get

```csharp
Response response;

try
{
    response = await Rest.GetAsync("www.your.api/endpoint");
}
catch (Exception e)
{
    Debug.LogError(e);
}

Debug.Log($"[{response.ResponseCode}] {response.ResponseBody}");
```

### Post

```csharp
Response response;
byte[] someBinaryData = new byte[0];

try
{
    response = await Rest.PostAsync("www.your.api/endpoint", someBinaryData);
}
catch (Exception e)
{
    Debug.LogError(e);
}

Debug.Log($"[{response.ResponseCode}] {response.ResponseBody}");
```

### Put

```csharp
Response response;
string jsonData = "{\"data\":\"content\"}";

try
{
    response = await Rest.PutAsync("www.your.api/endpoint", jsonData);
}
catch (Exception e)
{
    Debug.LogError(e);
}

Debug.Log($"[{response.ResponseCode}] {response.ResponseBody}");
```

### Patch

```csharp
Response response;
string jsonData = "{\"data\":\"content\"}";

try
{
    response = await Rest.PatchAsync("www.your.api/endpoint", jsonData);
}
catch (Exception e)
{
    Debug.LogError(e);
}

Debug.Log($"[{response.ResponseCode}] {response.ResponseBody}");
```

### Delete

```csharp
Response response;

try
{
    response = await Rest.DeleteAsync("www.your.api/endpoint");
}
catch (Exception e)
{
    Debug.LogError(e);
}

Debug.Log($"[{response.ResponseCode}] {response.ResponseBody}");
```

### Multimedia

#### Caching

```csharp
// cache directory defaults to {Application.temporaryCachePath}/download_cache/
// it is currently not possible to set this value, but is likely a nice feature request.
Debug.Log(Rest.DownloadCacheDirectory);

var uri = "www.url.to/remote/resource";

if (Rest.TryGetDownloadCacheItem(uri, out var cachedFilePath))
{
    // the local downloaded file in the cache
    Debug.Log(cachedFilePath);

    // Delete a locally cached item.
    if (Rest.TryDeleteCacheItem(uri))
    {
        Debug.Log($"Deleted {cachedFilePath}");
    }
}

// Clear the whole download cache directory
Rest.DeleteDownloadCache();
```

#### Files

```csharp
var downloadedFilePath = await Rest.DownloadFileAsync("www.your.api/your_file.pdf");

if (!string.IsNullOrWhiteSpace(downloadedFilePath))
{
    Debug.Log(downloadedFilePath);
}
```

#### Textures

> Pro Tip: This also works with local file paths to load textures async at runtime!

```csharp
var texture = await Rest.DownloadTextureAsync("www.your.api/your_file.png");

if (texture != null)
{
    // assign it to your renderer
    rawImage.texture = texture;
}
```

#### Audio

> Pro Tip: This also works with local file paths to load audio clips async at runtime!

```csharp
var audioClip = await Rest.DownloadAudioClipAsync("www.your.api/your_file.ogg");

if (audioClip != null)
{
    // assign it to your audio source
    audioSource.clip = audioClip;
    audioSource.PlayOneShot(audioClip);
}
```

#### Asset Bundles

```csharp
var assetBundle = await Rest.DownloadAssetBundleAsync("www.your.api/asset.bundle");

if (assetBundle != null)
{
    var cube = bundle.LoadAsset<GameObject>("Cube");
    Instantiate(cube);
}
```
