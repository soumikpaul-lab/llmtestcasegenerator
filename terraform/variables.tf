variable "bucket_name" {
  description = "S3 bucket to store PDF files"
  type        = string
}

variable "region" {
  description = "AWS Region"
  type        = string
  default     = "us-west-2"
}
