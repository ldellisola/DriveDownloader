# Drive Downloader

This app will clone a folder from Google Drive to your own computer.

## Prerequisites

- To use this you must have your own API key. Follow the instructions [here](https://www.iperiusbackup.net/en/how-to-enable-google-drive-api-and-get-client-credentials/) but choose `Desktop Application` instead of `Web Application`. Also don't forget to add yourself as a test user.
- Install docker

## How to use

- with `docker run`

```bash
$ docker pull ghcr.io/ldellisola/drive_downloader-amd64:latest
$ docker run --volume=/local/path:/data -it -d --name=drive ldellisola/drive_downloader-x64:latest
$ docker attach drive
```
- a one-liner with `docker run`

```bash
$ docker pull ghcr.io/ldellisola/drive_downloader-amd64:latest
$ docker attach $( docker run --volume=/local/path:/data -it -d ldellisola/drive_downloader-x64:latest) 
```

- with docker compose:

```
version: "3"
services:
  drive_downloader:
    image: ghcr.io/ldellisola/drive_downloader-amd64:latest
    stdin_open: true # docker run -i
    tty: true        # docker run -t
    volumes:
      - /path/to/local/folder:/data
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
- `get Errors`: It prints all the download errors.

If you container was stopped or closed, restart it using:

```bash
$ docker start drive
```

