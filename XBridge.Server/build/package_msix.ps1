# Helper script to pad files to desired size
param(
[string]$FilePath,
[int]$TargetSizeKB
)


$TargetBytes = $TargetSizeKB * 1024
$CurrentSize = (Get-Item $FilePath).Length


if ($CurrentSize -lt $TargetBytes) {
$PaddingSize = $TargetBytes - $CurrentSize
$fs = [System.IO.File]::OpenWrite($FilePath)
$fs.Seek(0, [System.IO.SeekOrigin]::End) | Out-Null
$fs.Write((New-Object Byte[] $PaddingSize), 0, $PaddingSize)
$fs.Close()
Write-Host "$FilePath padded to $TargetSizeKB KB"
} else {
Write-Host "$FilePath is already larger than target size."
}