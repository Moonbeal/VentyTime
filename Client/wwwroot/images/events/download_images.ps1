$categories = @{
    "tech" = "https://source.unsplash.com/800x600/?technology,programming"
    "business" = "https://source.unsplash.com/800x600/?business,office"
    "art" = "https://source.unsplash.com/800x600/?art,culture"
    "sport" = "https://source.unsplash.com/800x600/?sports,fitness"
}

foreach ($category in $categories.Keys) {
    for ($i = 1; $i -le 5; $i++) {
        $url = $categories[$category]
        $outputFile = "$category$i.jpg"
        Write-Host "Downloading $outputFile..."
        Invoke-WebRequest -Uri $url -OutFile $outputFile
        Start-Sleep -Seconds 1
    }
}

Write-Host "All images downloaded successfully!"
