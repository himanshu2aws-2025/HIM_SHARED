# -----------------------------------
# Capstone Project - Load and Inspect Dataset
# -----------------------------------

# Define dataset path
data_path = os.path.join("Data", "capstone1_rag_dataset.csv")

# Load dataset
try:
    df = pd.read_csv(data_path)
    print("✅ Dataset loaded successfully!")
except FileNotFoundError:
    print("❌ Error: Dataset file not found. Please check the path.")
    raise

# Display dataset info
print("\n📊 Dataset Info:")
print(df.info())

print("\n🔍 First 5 Rows:")
display(df.head())

print("\n🧩 Missing Values:")
print(df.isnull().sum())
