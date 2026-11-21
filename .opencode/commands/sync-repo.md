---
description: 루트 저장소와 서브모듈 전체 Git 동기화 (sync-repo)
---

루트 저장소와 submodule 전체의 Git pull, commit, push 동기화를 위해 `sync-repo` 스킬을 실행하라.

대상 경로 인자가 있으면 그 경로에서, 없으면 이 명령을 호출한 repository context에서 `git rev-parse --show-toplevel`을 read-only로 실행해 canonical Git root를 정확히 하나 해석하라. 해석 실패나 인자가 Git 저장소 밖을 가리키면 Git mutation을 시작하지 말고 실패를 보고하라. 확인한 canonical root를 시작 보고와 artifact 경로에 기록하고, `Invoke-SyncRepo.ps1`에는 항상 `-RepositoryRoot <canonical-root>`를 명시적으로 전달하라. 명령 실행 중 현재 작업 디렉터리를 다른 저장소로 바꿔 대상이 바뀌게 해서는 안 된다. $ARGUMENTS