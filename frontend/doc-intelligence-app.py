import streamlit as st
import datetime
import requests
import argparse

# Parse command-line arguments for API base URL
parser = argparse.ArgumentParser()
parser.add_argument(
    "--api-base-url",
    dest="api_base_url",
    type=str,
    default="http://localhost:5252",
    help="Base URL for backend API, e.g., https://api.example.com"
)
# Ignore Streamlit flags
args, _ = parser.parse_known_args()
API_BASE_URL = args.api_base_url.rstrip("/")

# Backend API endpoints
API_LIST_URL = f"{API_BASE_URL}/api/benefit/docs"
API_UPLOAD_URL = f"{API_BASE_URL}/api/benefit/doc/upload"
MAX_UPLOAD_SIZE = 2 * 1024 * 1024  # 2 MB limit

# Status mapping
STATUS_MAP = {
    0: "Not Started",
    1: "In Queue",
    2: "Processing",
    3: "Complete",
    4: "Failed",
}

# Initialize refresh counter in session state
if "refresh" not in st.session_state:
    st.session_state.refresh = 0

# Page config
st.set_page_config(page_title="Document Intelligence App", layout="wide")

# Title
st.title("Document Intelligence")

# Custom CSS for styling file uploader and action buttons
st.markdown(
    """
    <style>
    .stFileUploader div[data-testid="stFileUploaderDropzone"] {
        background-color: #f0f2f6;
        border: 2px dashed #4CAF50;
        border-radius: 8px;
        padding: 20px;
    }
    table { width: 100%; border-collapse: collapse; margin-top: 20px; }
    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
    th { background-color: #000048; color: white; }
    tr:nth-child(even) { background-color: #f9f9f9; }
    .action-btn {
        display: inline-block;
        width: 32px;
        height: 32px;
        border-radius: 50%;
        line-height: 32px;
        text-align: center;
        color: white;
        margin: 4px;
        font-size: 16px;
        text-decoration: none;
        transition: transform 0.2s;
    }
    .action-btn:hover { transform: scale(1.5); }
    .testcases { background-color: #4CAF50; }
    .benefits { background-color: #2196F3; }
    </style>
    """,
    unsafe_allow_html=True
)

# File uploader
uploaded_file = st.file_uploader(
    "Upload a benefit document (PDF only, max 2 MB)",
    type=["pdf"],
    accept_multiple_files=False,
)
if uploaded_file:
    if uploaded_file.size > MAX_UPLOAD_SIZE:
        size_mb = uploaded_file.size / (1024 * 1024)
        st.error(f"File too large: {size_mb:.2f} MB. Maximum is 2 MB.")
    else:
        with st.spinner("Uploading file..."):
            try:
                files = {"file": (uploaded_file.name, uploaded_file, "application/pdf")}
                resp = requests.post(API_UPLOAD_URL, files=files)
                resp.raise_for_status()
                st.success("File uploaded successfully!")
                st.session_state.refresh += 1
            except Exception as e:
                st.error(f"Upload failed: {e}")

# Uploaded Documents section with refresh button on top-right
st.subheader("Uploaded Documents")
col_main, col_refresh = st.columns([9,1])
with col_main:
    pass
with col_refresh:
    if st.button("🔄 Refresh", help="Reload table", key="refresh_btn", type="secondary"):
        st.session_state.refresh += 1

# Helper functions

def parse_dt(iso_str):
    if not iso_str:
        return datetime.datetime.min
    for fmt in ("%Y-%m-%dT%H:%M:%S.%fZ", "%Y-%m-%dT%H:%M:%SZ"):
        try:
            return datetime.datetime.strptime(iso_str, fmt)
        except ValueError:
            continue
    return datetime.datetime.min


def format_dt(dt_obj):
    if dt_obj == datetime.datetime.min:
        return "-"
    return dt_obj.strftime("%B %d, %Y %I:%M %p")

# Fetch and render documents (refresh triggers rerun)
with st.spinner("Loading documents..."):
    try:
        docs = requests.get(API_LIST_URL).json()
    except Exception as e:
        st.error(f"Error fetching documents: {e}")
        docs = []

# Sort by uploadDateTime descending
docs_sorted = sorted(
    docs,
    key=lambda d: d.get("uploadDateTime", ""),
    reverse=True
)

# Display table or empty state
if docs_sorted:
    rows = []
    for d in docs_sorted:
        name = d.get("documentName", "N/A")
        raw_status = d.get("documentProcessStatus")
        try:
            code = int(raw_status)
            status = STATUS_MAP.get(code, "-")
        except:
            code = None
            status = raw_status or "-"
        dt_obj = parse_dt(d.get("uploadDateTime", ""))
        human = format_dt(dt_obj)
        actions = ""
        if code == 3:
            url = f"{API_BASE_URL}/api/benefit/testcase/download?fileName={name}"
            actions = f"<a href='{url}' class='action-btn testcases' title='Download Test Cases'>📝</a>"
        rows.append(f"<tr><td>{name}</td><td>{status}</td><td>{human}</td><td style='text-align:center;'>{actions}</td></tr>")
    table_html = (
        "<table><thead><tr>"
        "<th>Document Name</th><th>Status</th><th>Uploaded Date</th><th>Actions</th>"
        "</tr></thead><tbody>" + "".join(rows) + "</tbody></table>"
    )
    st.markdown(table_html, unsafe_allow_html=True)
else:
    st.info("No documents uploaded")
