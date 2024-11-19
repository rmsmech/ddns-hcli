# Pre requisite 
**dotnet-sdk-8.0** is required to run this application. Install the package using `yum` or `dnf` or `apt-get`.

# 01: Download into linux using below command.

```wget https://github.com/rmsmech/ddns-hcli/releases/latest/download/ddns-hcli.zip```

_Install wget if not available._

# 02 : _(Optional)_ Move the zip file to a folder in root.

Make a directory in root

```mkdir /ddns-hcli```

From initial download location, copy the zip file to new location. (I downloaded it into home path of the current user)

```mv ~/ddns-hcli.zip /ddns-hcli/ddns-hcli.zip```

# 03 : Extract the zip to the folder

```unzip ddns-hcli.zip -d src```

Above command will unzip the zip file into a folder called src. If you wish to extract to same folder, just try `unzip ddns-hclip.zip`

_Install unzip, if not available_

# 04 : Create Config Files.

This step can be done after Step 05 (incase you wish to export the default configs to the folders).

**Create a file called "ddns.conf" with below information**.

Obtain zone-id , token from your cloudflare account and add them. Add all the records (A type only supported) for which the ip address has to be updated.

```JSON
[
  {
    "zone-id": "CHANGE_ME_ZONE_ID",
    "token": "CHANGE_ME_TOKEN",
    "records": "CHANGE_ME_RECORD_1,CHANGE_ME_RECORD_2" 
  },
  {
    "zone-id": "CHANGE_ME_ZONE_ID_2",
    "token": "CHANGE_ME_TOKEN_2",
    "records": "CHANGE_ME_RECORD_3,CHANGE_ME_RECORD_4"
  }
]
```
# 05 : _(Optional)_ Test the application
If all the steps are done correctly, at this point, you will be able to run below command to initiate the application and see the results in the terminal.

`dotnet ddns-hcli.dll -v`

-v enables verbose mode, so that the log is printed on the terminal directly.

If the application runs and you are able to see the updates in terminal, go ahead to next step.

# 06 : Create a sysmtemd service

Create a systemd service to ensure that the application runs on startup.
