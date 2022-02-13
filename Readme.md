# Troubleshooting Proxy
This C# program acts as a proxy between a local program and a remote server. The local program is configured to talk to this proxy instead of the remote server, which allows you to view and modify all communications between the two. It was written to help me troubleshoot a specific problem and so is a little rough around the edges - the problem was resolved and development stopped!

## General Info
I created this tool to troubleshoot a problem with an old application that used simple http requests to download data from a remote server. After years of working correctly, it suddenly started getting corrupted when downloading new data. 

Fiddler and WireShark let me see the contents of the communication, but didn’t give a lot of control over adjusting it, to test whether what I thought was causing the problem was the actual cause. It looked like an intermediate host was inspecting the headers, seeing something that it thought was missing and adding it. The local application didn’t expect an extra line in the returned data when importing it, throwing off all subsequent fields by one, resulting in corrupted data.

Surprisingly, the proxy program automatically corrected this addition, just by my copying the headers and content from the response from the remote server to a new response and forwarding that to the local program.

Thus, as it is, this program only changes the host for the request and nothing in the response, but the entry points exist for making adjustments - see GetAdjustedRequest and GetAdjustedResponse. You could also completely replace the request or response - see SendFakeResponse for an example

## Getting Started - Basic Usage
- Copy this repo to your machine.
- Configure the application that you want to monitor so that it communicates with localhost (127.0.0.1) and the configured port (default: 9090), rather than the actual server.
- Put the actual server url (i.e. www.myserver.com) in the app.config file and save it. Rebuild the project.
- From a command prompt, run the executable. If you want to save the communications to a file, pipe the output to a file. i.e. ToolkitProxy.exe > output.txt.
- Execute whatever functionality you want to monitor, in the application.
- Hit Ctrl-C in the command prompt, to close the proxy.
- View the results on screen, or in the file.
- If you want to adjust the request or response, change the code in GetAdjustedRequest or GetAdjustedResponse and recompile.

### Prerequisites
VS Code, C# 9, .Net core 5 sdk, .Net core 5 runtime.

## Authors

* **Greg Walker** - *Initial work* - (https://github.com/gregw18)


## License

MIT
