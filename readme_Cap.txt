# -----------------------------------
# Capstone Project - Create and Store Embeddings
# -----------------------------------

from langchain_openai import OpenAIEmbeddings
from langchain_community.vectorstores import FAISS

# Read environment variables
azure_api_key = os.getenv("AZURE_OPENAI_API_KEY")
azure_endpoint = os.getenv("AZURE_OPENAI_ENDPOINT")

if not azure_api_key or not azure_endpoint:
    raise ValueError("❌ Missing Azure OpenAI credentials. Please set AZURE_OPENAI_API_KEY and AZURE_OPENAI_ENDPOINT.")

# Initialize Azure OpenAI Embeddings
embeddings = OpenAIEmbeddings(
    model="text-embedding-3-small",  # Or use 'text-embedding-ada-002' depending on setup
    openai_api_key=azure_api_key,
    openai_api_base=azure_endpoint,
)

# Create FAISS vector store from chunks
print("🔄 Creating embeddings, this may take a minute...")
vector_store = FAISS.from_texts(chunks, embedding=embeddings)

# Save the FAISS index locally (Databricks path)
faiss_index_path = "Data/faiss_index"
vector_store.save_local(faiss_index_path)

print(f"✅ Embeddings created and FAISS index saved at: {faiss_index_path}")
