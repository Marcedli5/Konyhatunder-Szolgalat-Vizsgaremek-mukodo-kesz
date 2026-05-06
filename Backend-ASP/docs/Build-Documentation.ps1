param(
    [string]$SourcePath = (Join-Path $PSScriptRoot "vizsgaremek-dokumentacio.html"),
    [string]$PdfPath = (Join-Path $PSScriptRoot "vizsgaremek-dokumentacio.pdf")
)

$word = $null
$document = $null
$tempDocx = Join-Path $PSScriptRoot "vizsgaremek-dokumentacio.docx"

try {
    if (-not (Test-Path -LiteralPath $SourcePath)) {
        throw "A forrasfajl nem talalhato: $SourcePath"
    }

    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0

    $document = $word.Documents.Open($SourcePath)
    $document.SaveAs([ref]$tempDocx, [ref]16)
    $document.ExportAsFixedFormat($PdfPath, 17)
}
finally {
    if ($document -ne $null) {
        $document.Close([ref]$false)
    }

    if ($word -ne $null) {
        $word.Quit()
    }

    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
}
