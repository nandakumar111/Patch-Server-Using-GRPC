```Installer.proto```

- File Copy copies .exe files to the remote machines.
- Installer.proto use powershell, Receiver should be able to install the copied .exe on the remote machines and wait for the installation to complete.
- once complete delete the file and send message succesfully installed back to the sender

## Receiver Project Info
- Run `Terminal` as a Administrator
- Go to project build location 
```sh
cd <BUILD_FILE_LOCATION>
```
- Run `Receiver.exe`
