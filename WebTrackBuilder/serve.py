#!/usr/bin/env python3
"""
FormulaFun Track Builder - Local HTTP Server

Start from the FormulaFun project root:
    python3 WebTrackBuilder/serve.py

Then open: http://localhost:8000/WebTrackBuilder/index.html
"""
import http.server
import os

os.chdir(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
print(f"Serving from: {os.getcwd()}")
print(f"Open: http://localhost:8000/WebTrackBuilder/index.html")
http.server.HTTPServer(('', 8000), http.server.SimpleHTTPRequestHandler).serve_forever()
