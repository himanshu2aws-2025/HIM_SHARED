# -----------------------------------
# Capstone Project - Text Splitting for RAG
# -----------------------------------

from langchain.text_splitter import RecursiveCharacterTextSplitter

# Ensure your dataset has a text column (update if name differs)
text_column = "text"  # Change this if your CSV uses a different column name

if text_column not in df.columns:
    raise ValueError(f"❌ Column '{text_column}' not found. Available columns: {list(df.columns)}")

# Combine all text entries into one large string (if multiple rows)
all_text = "\n\n".join(df[text_column].astype(str).tolist())

# Initialize text splitter
text_splitter = RecursiveCharacterTextSplitter(
    chunk_size=1000,     # Each chunk has up to 1000 characters
    chunk_overlap=100,   # Overlap ensures context continuity
    separators=["\n\n", "\n", ".", "!", "?", " ", ""]
)

# Split text into chunks
chunks = text_splitter.split_text(all_text)

print(f"✅ Successfully split text into {len(chunks)} chunks.")
print("\n🔍 Sample Chunks:")
for i, chunk in enumerate(chunks[:3]):
    print(f"\nChunk {i+1}:\n{chunk[:300]}...")
