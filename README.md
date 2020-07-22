# spisum-emailbox

## Version

v1.0-beta

## Prerequisities

This container have to be deployed at least 10 minutes after first deploy of https://github.com/ISFG/alfresco-core.

- GIT
- Docker
- Docker-compose

## How to run application

In this file **ISFG.EmailBox/ConfigurationFiles/EmailConfiguration.json** set your credentials for one or more e-mail accounts
```json
[
    {
        "Username": "email@provider.domain",
        "Password": "password",
        "DisplayName": "name",
        "Pop3": {
            "Port": 995,
            "Host": "pop.provider.domain",
            "UseSSL": true
        },
        "Stmp": {
            "Port": 465,
            "Host": "smtp.provider.domain",
            "UseSSL": true
        }
    }
]
```

```bash
$ git clone https://github.com/ISFG/spisum-emailbox.git -b master --single-branch spisum-emailbox
$ cd spisum-emailbox 
$ sudo docker-compose up -d --build
```