workflow "Release dabutvin/imgbot-compressimages" {
  on = "release"
  resolves = ["GitHub Action for Docker-1"]
}

action "Docker Registry" {
  uses = "actions/docker/login@8cdf801b322af5f369e00d85e9cf3a7122f49108"
  secrets = ["DOCKER_USERNAME", "DOCKER_PASSWORD"]
}

action "GitHub Action for Docker" {
  uses = "actions/docker/cli@8cdf801b322af5f369e00d85e9cf3a7122f49108"
  needs = ["Docker Registry"]
  args = "build -f Dockerfile.CompressImages . -t imgbot-compressimages"
}

action "Docker Tag" {
  uses = "actions/docker/tag@8cdf801b322af5f369e00d85e9cf3a7122f49108"
  needs = ["GitHub Action for Docker"]
  args = "imgbot-compressimages dabutvin/imgbot-compressimages"
}

action "GitHub Action for Docker-1" {
  uses = "actions/docker/cli@8cdf801b322af5f369e00d85e9cf3a7122f49108"
  needs = ["Docker Tag"]
  args = "push dabutvin/imgbot-compressimages"
}
