# code.ipynb

# === Import Libraries ===
from langchain_openai import AzureChatOpenAI
from langchain.prompts import ChatPromptTemplate
from langchain.chains import LLMChain
import pandas as pd
import os

# === Load Environment Variables ===
from dotenv import load_dotenv
load_dotenv()

# === Setup Model ===
llm = AzureChatOpenAI(
    azure_deployment="gpt-4o-mini",
    api_version="2024-08-01-preview"
)
print("✅ LLM initialized successfully")


# === Load Dataset ===
data_path = "./Data/capstone1_rag_dataset.csv"
df = pd.read_csv(data_path)
print("✅ Dataset loaded successfully")
df.head()


# === Sample Prompt Template ===
prompt = ChatPromptTemplate.from_template(
    "Based on the dataset, summarize what this dataset is about in 3 sentences."
)

chain = LLMChain(llm=llm, prompt=prompt)
response = chain.run(input=[])
print("🧩 Sample Model Response:")
print(response)


# === Validate using Test Questions ===
test_questions = pd.read_csv("./Data/capstone1_rag_test_questions.csv")
print(f"Loaded {len(test_questions)} test questions")

sample_question = test_questions.iloc[0]["question"]
print("Q:", sample_question)
print("A:", chain.run(input=sample_question))


✅ Step 3: Run the Notebook

Launch Jupyter:

jupyter notebook


Open code.ipynb.

Run all cells in order.



# -----------------------------------
# Capstone Project - Setup Verification
# -----------------------------------

# Import core libraries
import os
import sys
import pandas as pd
import numpy as np

# LangChain and Azure-related imports
from langchain_openai import OpenAIEmbeddings, ChatOpenAI
from langchain_community.vectorstores import FAISS
from langchain_community.document_loaders import CSVLoader
from langchain.text_splitter import RecursiveCharacterTextSplitter

# Azure identity and environment management
from azure.identity import DefaultAzureCredential

# Other helpers
import warnings
warnings.filterwarnings("ignore")

print("✅ Environment setup successful!")
print(f"Python version: {sys.version}")
print(f"Pandas version: {pd.__version__}")
print(f"Current working directory: {os.getcwd()}")
