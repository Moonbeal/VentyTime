$imageUrls = @{
    "meetup.jpg" = "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?w=1600&h=900"
    "technology-event.jpg" = "https://images.unsplash.com/photo-1505373877841-8d25f7d46678?w=1600&h=900"
    "ai-workshop.jpg" = "https://images.unsplash.com/photo-1485827404703-89b55fcc595e?w=1600&h=900"
    "webinar.jpg" = "https://images.unsplash.com/photo-1609921212029-bb5a28e60960?w=1600&h=900"
    "workshop.jpg" = "https://images.unsplash.com/photo-1524178232363-1fb2b075b655?w=1600&h=900"
    "conference.jpg" = "https://images.unsplash.com/photo-1505373877841-8d25f7d46678?w=1600&h=900"
    "hackathon.jpg" = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?w=1600&h=900"
    "networking.jpg" = "https://images.unsplash.com/photo-1519167758481-83f550bb49b3?w=1600&h=900"
    "seminar.jpg" = "https://images.unsplash.com/photo-1475721027785-f74eccf877e2?w=1600&h=900"
    "training.jpg" = "https://images.unsplash.com/photo-1524178232363-1fb2b075b655?w=1600&h=900"
    "startup-pitch.jpg" = "https://images.unsplash.com/photo-1542744173-8e7e53415bb0?w=1600&h=900"
    "product-launch.jpg" = "https://images.unsplash.com/photo-1551818255-e6e10975bc17?w=1600&h=900"
    "career-fair.jpg" = "https://images.unsplash.com/photo-1521737711867-e3b97375f902?w=1600&h=900"
    "panel-discussion.jpg" = "https://images.unsplash.com/photo-1475721027785-f74eccf877e2?w=1600&h=900"
    "awards-ceremony.jpg" = "https://images.unsplash.com/photo-1531545514256-b1400bc00f31?w=1600&h=900"
    "charity-gala.jpg" = "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?w=1600&h=900"
    "team-building.jpg" = "https://images.unsplash.com/photo-1517048676732-d65bc937f952?w=1600&h=900"
    "industry-expo.jpg" = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=1600&h=900"
    "research-symposium.jpg" = "https://images.unsplash.com/photo-1517048676732-d65bc937f952?w=1600&h=900"
    "innovation-summit.jpg" = "https://images.unsplash.com/photo-1505373877841-8d25f7d46678?w=1600&h=900"
}

$outputPath = "wwwroot\images\events"

foreach ($image in $imageUrls.GetEnumerator()) {
    $outputFile = Join-Path $outputPath $image.Key
    Write-Host "Downloading $($image.Value) to $outputFile"
    Invoke-WebRequest -Uri $image.Value -OutFile $outputFile
    Start-Sleep -Seconds 1  # Add delay to avoid rate limiting
}
