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
# Use parse_known_args to ignore Streamlit's own flags
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
# Page config
st.set_page_config(page_title="Document Intelligence App", layout="centered")

# Title
st.title("Document Intelligence")

st.markdown(
    """
    <style>
    /* File uploader dropzone */
    .stFileUploader div[data-testid="stFileUploaderDropzone"] {
        background-color: #f0f2f6;
        border: 2px dashed #4CAF50;
        border-radius: 8px;
        padding: 20px;
    }
    /* Table styling */
    table {
        width: 100%;
        border-collapse: collapse;
        margin-top: 20px;
    }
    th, td {
        border: 1px solid #ddd;
        padding: 8px;
        text-align: left;
    }
    th {
        background-color: #000048;
        color: white;
    }
    tr:nth-child(even) {background-color: #f9f9f9;}
    /* Round action buttons */
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
    }
    .testcases { background-color: #4CAF50; }
    .benefits { background-color: #2196F3; }
    </style>
    """,
    unsafe_allow_html=True
)

# File uploader
uploaded_file = st.file_uploader(
    "Upload a benefit document",
    type=["pdf"],
    accept_multiple_files=False,
)
if uploaded_file:
    # Enforce file size limit before uploading
    if uploaded_file.size > MAX_UPLOAD_SIZE:
        size_mb = uploaded_file.size / (1024 * 1024)
        st.error(f"File too large: {size_mb:.2f} MB. Maximum allowed size is 2 MB.")
    else:
        with st.spinner("Uploading file..."):
            try:
                files = {"file": (uploaded_file.name, uploaded_file, "application/pdf")}
                resp = requests.post(API_UPLOAD_URL, files=files)
                resp.raise_for_status()
                st.success("File uploaded successfully!")
                st.experimental_rerun()
            except Exception as e:
                st.error(f"Upload failed: {e}")

# Display uploaded documents table
st.subheader("Uploaded Documents")
def parse_dt(iso_str):
    if not iso_str:
        return datetime.datetime.min
    try:
        return datetime.datetime.strptime(iso_str, "%Y-%m-%dT%H:%M:%S.%fZ")
    except ValueError:
        try:
            return datetime.datetime.strptime(iso_str, "%Y-%m-%dT%H:%M:%SZ")
        except ValueError:
            return datetime.datetime.min


def format_dt(dt_obj):
    if dt_obj == datetime.datetime.min:
        return "-"
    return dt_obj.strftime("%B %d, %Y %I:%M %p")

# Fetch documents synchronously
docs = []
with st.spinner("Loading documents..."):
    try:
        resp = requests.get(f'{API_LIST_URL}')
        resp.raise_for_status()
        docs = resp.json()
    except Exception as e:
        st.error(f"Error fetching documents: {e}")
# Sort documents by upload date descending
try:
    docs_sorted = sorted(
        docs,
        key=lambda x: x.get("uploadDateTime", ""),
        reverse=True
    )
except Exception:
    docs_sorted = docs
# Display document table
if docs_sorted:
    rows_html = []
    # Build HTML table
    table_html = "<table>"
    table_html += (
        "<thead>"
        "<tr>"
        "<th>Document Name</th>"
        "<th>Status</th>"
        "<th>Uploaded Date</th>"
        "<th>Actions</th>"
        "</tr>"
        "</thead><tbody>"
    )
    for doc in docs_sorted:
        name = doc.get("documentName", "N/A")
        raw_status = doc.get("documentProcessStatus")
        try:
            code = int(raw_status)
            status_label = STATUS_MAP.get(code, "-")
        except Exception:
            status_label = raw_status or "-"

        dt_obj = parse_dt(doc.get("uploadDateTime", ""))
        human_date = format_dt(dt_obj)
        #Convert ISO date format to human readable format
        # Action URLs
        # Determine button state: enabled only if status is Complete (code == 3)
        enabled = (int(raw_status) == 3)
        if code == 3:
            tc_btn = f"<a href='{API_BASE_URL}/api/benefit/testcase/download?fileName={name}' class='action-btn testcases' title='Download Test Cases'>📝</a>"
            ben_btn = f"<a href='{'API_LIST_URL'}/{doc.get('id')}/benefits' class='action-btn benefits' title='Download Extracted Benefits'>💾</a>"
            actions_html = tc_btn #+ ben_btn
        else:
            actions_html = ""

        rows_html.append(
            f"<tr>"
            f"<td>{name}</td>"
            f"<td>{status_label}</td>"
            f"<td>{human_date}</td>"
            f"<td style='text-align:center;'>{actions_html}</td>"
            f"</tr>"
        )
    table_html = (
        "<table>"
        "<thead><tr>"
        "<th>Document Name</th><th>Status</th><th>Uploaded Date</th><th>Actions</th>"
        "</tr></thead><tbody>" + "".join(rows_html) + "</tbody></table>"
    )
    st.markdown(table_html, unsafe_allow_html=True)
else:
    st.info("No documents uploaded")



