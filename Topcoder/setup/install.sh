#!/usr/bin/bash

wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

apt update
sudo apt install gpg -y
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > packages.microsoft.gpg
sudo install -o root -g root -m 644 packages.microsoft.gpg /etc/apt/trusted.gpg.d/
sudo sh -c 'echo "deb [arch=amd64,arm64,armhf signed-by=/etc/apt/trusted.gpg.d/packages.microsoft.gpg] https://packages.microsoft.com/repos/code stable main" > /etc/apt/sources.list.d/vscode.list'
rm -f packages.microsoft.gpg

apt update
apt install i3 dmenu -y
mkdir ~/.config
mkdir ~/.config/i3
mkdir ~/.config/i3status
wget https://raw.github.com/eulerscheZahl/Topcoder/master/setup/i3_config -O ~/.config/i3/config
wget https://raw.github.com/eulerscheZahl/Topcoder/master/setup/i3status_config -O ~/.config/i3status/config

apt install apt-transport-https -y
apt update
apt install dotnet-sdk-6.0 -y
apt install code -y
code --install-extension ms-dotnettools.csharp
code --install-extension visualstudioexptteam.vscodeintellicode
code --install-extension vscode-icons-team.vscode-icons
code --install-extension kreativ-software.csharpextensions
apt install git -y
apt install meld -y

apt install openjdk-17-jre -y
apt install python3.9 -y
apt install pari-gp -y
apt install chromium-browser -y
apt install zsh -y
apt install parcellite -y

sh -c "$(wget https://raw.github.com/ohmyzsh/ohmyzsh/master/tools/install.sh -O -)"
