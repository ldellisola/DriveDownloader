# Drive Downloader

This app will clone a folder from Google Drive to your own computer.

## Prerequisites

- To use this you must have your own API key. Follow the instructions [here](https://www.iperiusbackup.net/en/how-to-enable-google-drive-api-and-get-client-credentials/) but choose `Desktop Application` instead of `Web Application`. Also don't forget to add yourself as a test user.
- Install docker

## How to use

```bash
$ docker pull ghcr.io/ldellisola/drive_downloader-amd64:latest
$ docker run --volume=/local/path:/data -it -d --name=drive ldellisola/drive_downloader-x64:latest
$ docker attach drive
```
> To use the ARM image (on MacOS) change it to drive_downloader-arm

 The first time it will ask for the Google Drive API credentials and data. After that it will start looking for your files, this may take a while, and then you will be prompted with a CLI.

Here are some important commands:

- `help`: It prints information about all the commands. If you pass another command as argument, it will only show information about that specific command.
- `start`: starts running the download threads
- `stop`: stops running the download threads.
- `set DownloadThreads VALUE`: It says how many threads are used to download the files.
- `quit`: It closes the program.
- `retry`: It lets you retry downloads.

If you container was stopped or closed, restart it using:

```bash
$ docker start drive
```

