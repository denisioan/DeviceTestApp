# Application test from CONSUMER point of view

Use Postman to launch commands.

## Token:
**POST http://localhost:8080/token**
in the Body add the parameters:

client_id: armorsafe_client_id
client_secret: armorsafe_client_secret

Response example:
{
    "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.                      eyJzdWIiOiJhcm1vcnNhZmVfY2xpZW50X2lkIiwianRpIjoiYzg4ZWQ0OWItNmE1OS00ZDY4LTljNzUtYmE1YjNhYjcwNTlhIiwiZXhwIjoxNzI4Mzk0NjQwLCJpc3MiOiJodHRwOi8vbG9jYWxob3N0OjgwODAiLCJhdWQiOiJteV9hcGlfYXVkaWVuY2UifQ.7uhqsLDmkNdDrPp9N1jQW_5I0u_hmGCI9U_RXKnRipo",
    "token_type": "Bearer",
    "expires_in": 3600
}

We need to use this token to issue protected commands



## Status:
**GET http://localhost:8080/status**
In this case we need a Authorization field in the Headers like this:
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhcm1vcnNhZmVfY2xpZW50X2lkIiwianRpIjoiYzg4ZWQ0OWItNmE1OS00ZDY4LTljNzUtYmE1YjNhYjcwNTlhIiwiZXhwIjoxNzI4Mzk0NjQwLCJpc3MiOiJodHRwOi8vbG9jYWxob3N0OjgwODAiLCJhdWQiOiJteV9hcGlfYXVkaWVuY2UifQ.7uhqsLDmkNdDrPp9N1jQW_5I0u_hmGCI9U_RXKnRipo

Response example:
{
    "status": "operational"
}


## Deposit:
**POST http://localhost:8080/deposit**
In the body we need the raw: 
{
  "amount": 200
}

Response example:
{
    "Success": true,
    "Message": "Deposited 200 units successfully."
}


## Thing-description
**GET http://localhost:8080/thing-description**
In this case the command is not protected for discovery purposes so we don't need to add the token bearer

Response example:
{
    "context": "https://www.w3.org/2019/wot/td/v1",
    "id": "urn:dev:wot:cashdepositdevice-1234",
    "title": "Cash Deposit Device",
    "description": "A Web of Things test application for a cash deposit device.",
    "securityDefinitions": {
        "oauth2_sc": {
            "scheme": "oauth2",
            "flow": "client_credentials",
            "token": "http://localhost:8080/token",
            "scopes": [
                "deposit",
                "status"
            ]
        }
    },
    "security": [
        "oauth2_sc"
    ],
    "properties": {
        "status": {
            "type": "string",
            "description": "The current status of the device",
            "forms": [
                {
                    "href": "http://localhost:8080/status",
                    "contentType": "application/json",
                    "op": [
                        "readproperty"
                    ]
                }
            ]
        }
    },
    "actions": {
        "deposit": {
            "description": "Deposit an amount of money into the cash deposit device.",
            "input": {
                "type": "object",
                "properties": {
                    "amount": {
                        "type": "integer",
                        "minimum": 1
                    }
                }
            },
            "forms": [
                {
                    "href": "http://localhost:8080/deposit",
                    "contentType": "application/json",
                    "op": [
                        "invokeaction"
                    ]
                }
            ]
        }
    }
}

## Test events
From command line send the command: **websocat ws://localhost:8080/events/deviceFailure**
This will open a WebSocket and listen to the events "deviceFailure"