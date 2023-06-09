# com.utilities.rest

[![openupm](https://img.shields.io/npm/v/com.utilities.rest?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.utilities.rest/)

A Utilities.Rest package for the [Unity](https://unity.com/) Game Engine.

## Installing

### Via Unity Package Manager and OpenUPM

- Open your Unity project settings
- Select the `Package Manager`
![scoped-registries](Utilities.Rest/Packages/com.utilities.rest/Documentation~/images/package-manager-scopes.png)
- Add the OpenUPM package registry:
  - Name: `OpenUPM`
  - URL: `https://package.openupm.com`
  - Scope(s):
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
- [Rest Parameters](#rest-parameters)
- [Get](#get)
- [Post](#post)
  - [Server Sent Events](#server-sent-events)
- [Put](#put)
- [Patch](#patch)
- [Delete](#delete)
- [Download Multimedia](#multimedia)
  - [Caching](#caching)
  - [Files](#files)
  - [Textures](#textures)
  - [Audio](#audio)
    - [Streaming](#audio-streaming)
  - [Asset Bundles](#asset-bundles)

### Authentication

```csharp
// Get a basic auth header encoded to base64 string:
var basicAuthentication = Rest.GetBasicAuthentication("username", "password");

// Get a bearer auth token header
var bearerToken = Rest.GetBearerOAuthToken("authToken");
```

### Rest Parameters

Rest parameters have been bundled into a single object to make the method signatures a bit more uniform.

```csharp
var restParameters = new RestParameters(
    headers, // Optional, header information for the request.
    progressCallback, // Optional, Progress callback handler for the request.
    timeout, // Optional, time in seconds before the request expires. Default is -1.
    disposeDownloadHandler, // Optional, dispose the DownloadHandler. Default is true.
    disposeUploadHandler, // Optional, dispose the UploadHandler. Default is true.
    certificateHandler, // Optional, certificate handler for the request.
    disposeCertificateHandler); // Optional, dispose the CertificateHandler. Default is true.
var response = await Rest.GetAsync("www.your.api/endpoint", restParameters);
```

### Get

```csharp
var response = await Rest.GetAsync("www.your.api/endpoint");
// Validates the response for you and will throw a RestException if the response is unsuccessful.
response.Validate(debug: true);
```

### Post

```csharp
var form = new WWWForm();
form.AddField("fieldName", "fieldValue");
var response = await Rest.PostAsync("www.your.api/endpoint", form);
// Validates the response for you and will throw a RestException if the response is unsuccessful.
response.Validate(debug: true);
```

#### Server Sent Events

```csharp
var jsonData = "{\"data\":\"content\"}";
var response = await Rest.PostAsync("www.your.api/endpoint", jsonData, eventData => {
    Debug.Log(eventData);
});
// Validates the response for you and will throw a RestException if the response is unsuccessful.
response.Validate(debug: true);
```

### Put

```csharp
var jsonData = "{\"data\":\"content\"}";
var response = await Rest.PutAsync("www.your.api/endpoint", jsonData);
// Validates the response for you and will throw a RestException if the response is unsuccessful.
response.Validate(debug: true);
```

### Patch

```csharp
var jsonData = "{\"data\":\"content\"}";
var response = await Rest.PatchAsync("www.your.api/endpoint", jsonData);
// Validates the response for you and will throw a RestException if the response is unsuccessful.
response.Validate(debug: true);
```

### Delete

```csharp
var response = await Rest.DeleteAsync("www.your.api/endpoint", restParameters);
// Validates the response for you and will throw a RestException if the response is unsuccessful.
response.Validate(debug: true);
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
var audioClip = await Rest.DownloadAudioClipAsync("www.your.api/your_file.ogg", AudioType.OGGVORBIS);

if (audioClip != null)
{
// assign it to your audio source
audioSource.clip = audioClip;
    audioSource.PlayOneShot(audioClip);
}
```

##### Audio Streaming

Streams an audio file from disk or remote resource as soon as enough data has been loaded.

> Unsure if this is working correctly as Unity doesn't seem to respect streaming when setting [`DownloadHandlerAudioClip.streamAudio`](https://docs.unity3d.com/ScriptReference/Networking.DownloadHandlerAudioClip-streamAudio.html) to true.
> Seems to work better for local files on disk than remote resources.

```csharp
var audioClip = await Rest.StreamAudioAsync("local/path/to/your_file.ogg", AudioType.OGGVORBIS, onStreamPlaybackReady =>
{
    audioSource.PlayOneShot(onStreamPlaybackReady);
});
// you can assign the fully downloaded clip to your audio source if desired
audioSource.clip = audioClip;
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
