#!/bin/bash
set -euo pipefail

ensure_aws_cli() {
    if command -v aws >/dev/null 2>&1; then
        return
    fi

    dnf install -y awscli
}

fetch_secret_to_file() {
    local secret_name="$1"
    local aws_region="$2"
    local target_file="$3"
    local target_dir
    local temp_file

    target_dir="$(dirname "${target_file}")"
    mkdir -p "${target_dir}"
    temp_file="$(mktemp "${target_dir}/secret.XXXXXX")"

    aws secretsmanager get-secret-value \
        --secret-id "${secret_name}" \
        --region "${aws_region}" \
        --query SecretString \
        --output text > "${temp_file}"

    if [ ! -s "${temp_file}" ]; then
        echo "Fetched secret '${secret_name}' is empty."
        rm -f "${temp_file}"
        exit 1
    fi

    mv "${temp_file}" "${target_file}"
}

require_file_with_content() {
    local file_path="$1"
    local description="$2"

    if [ ! -f "${file_path}" ]; then
        echo "${description} not found at ${file_path}."
        exit 1
    fi

    if [ ! -s "${file_path}" ]; then
        echo "${description} is empty at ${file_path}."
        exit 1
    fi
}

reset_app_dir() {
    local app_dir="$1"

    rm -rf "${app_dir}"
    mkdir -p "${app_dir}"
}
