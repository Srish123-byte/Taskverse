@echo off
git config user.email "test@example.com"
git config user.name "Test User"
git config commit.gpgSign false
git config core.autocrlf false
del .git\index.lock
git --no-pager commit -m "temp_backup"
git --no-pager reset origin/develop
git --no-pager checkout -b develop
git --no-pager checkout -b gagan-report-feature
git --no-pager add .
git --no-pager commit -m "feat: Implement Reports UI and Exports"
git --no-pager checkout develop
git --no-pager reset --hard origin/develop
git --no-pager status
git --no-pager branch
echo DONE
