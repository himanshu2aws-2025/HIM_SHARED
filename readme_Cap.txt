import os
import json
import httpx
from dotenv import load_dotenv
from openai import AzureOpenAI

# Load environment variables
load_dotenv("./Data/UAIS_vars.env")

# Environment variables from UAIS_vars.env
AZURE_OPENAI_ENDPOINT = os.environ["MODEL_ENDPOINT"]
OPENAI_API_VERSION = os.environ["API_VERSION"]
EMBEDDINGS_DEPLOYMENT_NAME = os.environ["EMBEDDINGS_MODEL_NAME"]
PROJECT_ID = os.environ["PROJECT_ID"]

# ---- Get OAuth2 token from UAIS ----
auth_url = "https://api.uhg.com/oauth2/token"
scope = "https://api.uhg.com/.default"
grant_type = "client_credentials"

client_id = dbutils.secrets.get(scope="AIML_Training", key="client_id")
client_secret = dbutils.secrets.get(scope="AIML_Training", key="client_secret")

async def get_access_token():
    async with httpx.AsyncClient() as client:
        data = {
            "grant_type": grant_type,
            "scope": scope,
            "client_id": client_id,
            "client_secret": client_secret
        }
        headers = {"Content-Type": "application/x-www-form-urlencoded"}
        resp = await client.post(auth_url, headers=headers, data=data, timeout=120)
        token = resp.json().get("access_token")
        return token

import asyncio
access_token = asyncio.run(get_access_token())

# ---- Initialize Azure OpenAI Client ----
embeddings_client = AzureOpenAI(
    azure_endpoint=AZURE_OPENAI_ENDPOINT,
    api_version=OPENAI_API_VERSION,
    api_key=access_token
)

print("✅ Azure OpenAI client authenticated successfully.")

# Example: test embedding call (optional)
response = embeddings_client.embeddings.create(
    input="Hello world from Himanshu!",
    model=EMBEDDINGS_DEPLOYMENT_NAME
)

print("Embedding created successfully! Dimensions:", len(response.data[0].embedding))
