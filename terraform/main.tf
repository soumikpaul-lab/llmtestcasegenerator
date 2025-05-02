provider "aws" {
  region = var.region
  profile = "soumikp"
}

resource "aws_s3_bucket" "textract_bucket" {
  bucket = var.bucket_name
}

resource "aws_iam_role" "textract_role" {
  name = "textract-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17",
    Statement = [{
      Effect    = "Allow",
      Principal = {
        Service = "textract.amazonaws.com"
      },
      Action = "sts:AssumeRole"
    }]
  })
}

resource "aws_iam_role" "bedrock_role" {
  name = "bedrock-invoke-role"
  assume_role_policy = jsonencode({
    Version = "2012-10-17",
    Statement = [{
      Effect = "Allow",
      Principal = {
        Service = "bedrock.amazonaws.com"
      },
      Action = "sts:AssumeRole"
    }]
  })
}

resource "aws_iam_policy" "bedrock_invoke_policy" {
  name = "bedrock-invoke-policy"
  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Effect = "Allow",
        Action = [
          "bedrock:InvokeModel",
          "bedrock:InvokeModelWithResponseStream"
        ],
        Resource = "*"
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "attach" {
  role       = aws_iam_role.bedrock_role.name
  policy_arn = aws_iam_policy.bedrock_invoke_policy.arn
}

resource "aws_iam_role_policy" "textract_policy" {
  name = "textract-policy"
  role = aws_iam_role.textract_role.id

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Effect   = "Allow",
        Action   = [
          "s3:GetObject",
          "s3:PutObject"
        ],
        Resource = "${aws_s3_bucket.textract_bucket.arn}/*"
      },
      {
        Effect   = "Allow",
        Action   = [
          "textract:*"
        ],
        Resource = "*"
      }
    ]
  })
}
