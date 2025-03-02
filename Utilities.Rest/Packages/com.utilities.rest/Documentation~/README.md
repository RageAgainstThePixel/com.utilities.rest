# com.utilities.rest

[![Discord](https://img.shields.io/discord/855294214065487932.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/xQgMW9ufN4) [![openupm](https://img.shields.io/npm/v/com.utilities.rest?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.utilities.rest/) [![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.utilities.rest)](https://openupm.com/packages/com.utilities.rest/)

A Utilities.Rest package for the [Unity](https://unity.com/) Game Engine.

## Installing

Requires Unity 2021.3 LTS or higher.

The recommended installation method is though the unity package manager and [OpenUPM](https://openupm.com/packages/com.utilities.rest).

### Via Unity Package Manager and OpenUPM

#### Terminal

```bash
openupm add com.utilities.rest
```

#### Manual

- Open your Unity project settings
- Select the `Package Manager`
![scoped-registries](images/package-manager-scopes.png)
- Add the OpenUPM package registry:
  - Name: `OpenUPM`
  - URL: `https://package.openupm.com`
  - Scope(s):
    - `com.utilities`
- Open the Unity Package Manager window
- Change the Registry from Unity to `My Registries`
- Add the `Utilities.Rest` package

### Via Unity Package Manager and Git url

> [!WARNING]
> This repo has dependencies on other repositories! You are responsible for adding these on your own.

- Open your Unity Package Manager
- Add package from git url: `https://github.com/RageAgainstThePixel/com.utilities.rest.git#upm`
  - [com.utilities.async](https://github.com/RageAgainstThePixel/com.utilities.async)
  - [com.utilities.extensions](https://github.com/RageAgainstThePixel/com.utilities.extensions)

---

## Documentation

This library aims to provide basic support for common RESTful state transactions with most web APIs.

Advanced features includes progress notifications, authentication and native multimedia downloads of asset bundles, textures, audio clips with file caching.

### Table of contents

- [Authentication](#authentication)
- [Rest Parameters](#rest-parameters)
- [Get](#get)
- [Post](#post)
  - [Server Sent Events](#server-sent-events) :warning:
  - [Data Received Callbacks](#data-received-callbacks)
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

These methods will generate the necessary data to apply to your authentication headers, they DO NOT set the authentication for any Rest calls.

### Rest Parameters

Rest parameters have been bundled into a single object to make the method signatures a bit more uniform, these can be provided with all requests made using the `com.utilities.rest` library to control how the operation performs.

```csharp
var restParameters = new RestParameters(
    headers, // Optional, header information for the request.
    progressCallback, // Optional, Progress callback handler for the request.
    timeout, // Optional, time in seconds before the request expires. Default is -1.
    disposeDownloadHandler, // Optional, dispose the DownloadHandler. Default is true.
    disposeUploadHandler, // Optional, dispose the UploadHandler. Default is true.
    certificateHandler, // Optional, certificate handler for the request.
    disposeCertificateHandler, // Optional, dispose the CertificateHandler. Default is true.
    cacheDownloads, // Optional, cache downloaded content. Default is true.
    debug); // Optional, enable debug output of the request. Default is false.

// Rest call passing a pre-configured set of RestParameters
var response = await Rest.GetAsync("www.your.api/endpoint", restParameters);
```

If you require any of the above options when making calls, ensure to add a `RestParameters` construct to the request, as shown above.

> Pro Tip: If you have multiple requests using the same "Parameters", then cache a single instance of the `RestParameters` and use this for all calls instead of recreating it for each request.

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

> [!WARNING]
> This callback was recently refactored from `Action<Response, ServerSentEvent>` to `Func<Response, ServerSentEvent, Task>`. To update this callback without asynchronous calls, just add `await Task.CompletedTask;` at the end of your callback function.

```csharp
var jsonData = "{\"data\":\"content\"}";
var response = await Rest.PostAsync("www.your.api/endpoint", jsonData, async (sseResponse, ssEvent) => {
    Debug.Log(ssEvent);
    await Task.CompletedTask;
});
// Validates the response for you and will throw a RestException if the response is unsuccessful.
response.Validate(debug: true);
```

#### Data Received Callbacks

```csharp
var jsonData = "{\"data\":\"content\"}";
var response = await Rest.PostAsync("www.your.api/endpoint", jsonData, dataReceivedEventCallback => {
    // eventCallback type is Rest.Response
    Debug.Log(dataReceivedEventCallback.Body);
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
var response = await Rest.DeleteAsync("www.your.api/endpoint");
// Validates the response for you and will throw a RestException if the response is unsuccessful.
response.Validate(debug: true);
```

### Multimedia

#### Caching

You can set the parent directory of the download cache to one of the following predefined locations:

- `Application.temporaryCachePath` (default)
- `Application.persistentDataPath`
- `Application.dataPath`
- `Application.streamingAssetsPath`

The actual cache directory will be a subfolder named `download_cache` inside of the parent `DownloadCacheDirectory`.

```csharp
// cache directory defaults to {Application.temporaryCachePath}/download_cache/
Debug.Log(Rest.DownloadCacheDirectory);

// cache directory can be set to one of:
// Application.temporaryCachePath,
// Application.persistentDataPath,
// Application.dataPath,
// Application.streamingAssetsPath
Rest.DownloadLocation = Application.streamingAssetsPath;

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
else
{
    Debug.Log($"Unable to download from {uri}");
}

// Clear the whole download cache directory
Rest.DeleteDownloadCache();
```

#### Files

Download a file.

```csharp
var downloadedFilePath = await Rest.DownloadFileAsync("www.your.api/your_file.pdf");

if (!string.IsNullOrWhiteSpace(downloadedFilePath) && File.Exists(downloadedFilePath))
{
    Debug.Log(downloadedFilePath);
}
else
{
    Debug.LogError($"Failed to download file!");
}
```

Download a file, then return the raw byte data.

```csharp
var downloadedBytes = await Rest.DownloadFileBytesAsync("www.your.api/your_file.pdf");

if (downloadedBytes != null && downloadedBytes.Length > 0)
{
    // do some custom loading of your binary file.
    Debug.Log(downloadedBytes.Length);
}
else
{
    Debug.LogError($"Failed to download file!");
}
```

Download raw file bytes directly into memory.

> This request does not cache data

```csharp
var downloadedBytes = await Rest.DownloadBytesAsync("www.your.api/your_file.pdf");

if (downloadedBytes != null && downloadedBytes.Length > 0)
{
    Debug.Log(downloadedBytes.Length);
}
else
{
    Debug.LogError($"Failed to download file!");
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
else
{
    Debug.LogError($"Failed to download texture!");
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
else
{
    Debug.LogError($"Failed to download audio clip!");
}
```

##### Audio Streaming

Streams an audio file from disk or remote resource as soon as enough data has been loaded.

> Unsure if this is working correctly as Unity doesn't seem to respect streaming when setting [`DownloadHandlerAudioClip.streamAudio`](https://docs.unity3d.com/ScriptReference/Networking.DownloadHandlerAudioClip-streamAudio.html) to true.
> Seems to work better for local files on disk than remote resources.
> Seems to only work for mp3 files.

```csharp
var audioClip = await Rest.StreamAudioAsync("local/path/to/your_file.mp3", AudioType.MPEG, onStreamPlaybackReady =>
{
    audioSource.PlayOneShot(onStreamPlaybackReady);
});
// you can assign the fully downloaded clip to your audio source if desired
audioSource.clip = audioClip;
```

#### Asset Bundles

```csharp
var bundleOptions = new AssetBundleRequestOptions();
var assetBundle = await Rest.DownloadAssetBundleAsync("www.your.api/asset.bundle", bundleOptions);

if (assetBundle != null)
{
    var cube = assetBundle.LoadAsset<GameObject>("Cube");
    Instantiate(cube);
}
else
{
    Debug.LogError($"Failed to download and load asset bundle!");
}
```
