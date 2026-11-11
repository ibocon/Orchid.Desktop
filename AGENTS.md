# Orchid

## 공통 하네스 진입점

- 최상위 원칙: `lib/Garden.Harness/src/PRINCIPLES.md`
- 세부 규칙은 요청 유형에 따라 `lib/Garden.Harness/src/convention/`, `lib/Garden.Harness/src/guide/`, `lib/Garden.Harness/src/agents/skills/`, `lib/Garden.Harness/src/plan/skills/` 원문을 연다.
- 하네스 공통 진입 경로는 문서 하단 managed block을 따른다.

## Shared Agents

- 공통 서브 에이전트 role SoT는 `lib/Garden.Harness/src/agents/*.md`와 `lib/Garden.Harness/src/plan/agents/*.md`, 절차 원본은 `lib/Garden.Harness/src/agents/skills/*/SKILL.md`와 `lib/Garden.Harness/src/plan/skills/*/SKILL.md` 다.
- Claude Code 는 `.claude/agents/*.md` wrapper 를 통해 same role 을 사용한다.
- ChatGPT Codex 는 `.codex/agents/*.toml`과 `.codex/skills`를 통해 same role 을 native subagent로 사용한다.
- Gemini CLI 는 `GEMINI.md` 지침 세트를 통해 하네스 표준을 준수한다.
- OpenCode 는 `AGENTS.md` 지침 세트와 루트 `opencode.json` 공유 MCP 설정을 통해 하네스 표준을 준수한다.
- role 추가나 수정 후에는 `pwsh -File lib/Garden.Harness/src/agents/scripts/Sync-SharedAgents.ps1` 를 먼저 실행한다.

## 절대 규칙

- 항상 한국어로 응답한다
- Public API에는 반드시 XML 문서 주석을 포함한다
- Git index, `.git/index.lock`, ref update는 저장소 단위 공유 가변 상태로 취급한다
- 같은 저장소에서 Git mutation 명령(`git add`, `git commit`, `git merge`, `git rebase`, `git cherry-pick`, `git push`)을 다른 Git 명령과 병렬 실행하지 않는다

## Architecture

### Tech Stack

- .NET 9, C# 13
- .NET MAUI (Windows, macOS, Android, iOS), Blazor Hybrid
- Clean Architecture, CQRS, SignalR/WebSocket
- xUnit, FluentAssertions

### Solution Structure

```text
src/
  Core/                  # 4대 OS 공용 비즈니스 코어 (순수 C#)
  Desktop/               # .NET MAUI Blazor Hybrid (Win / Mac)
  Mobile/                # .NET MAUI Blazor Hybrid (AOS / iOS)
  Web/                   # Blazor WebAssembly 뷰어
  Service/               # Clean Architecture E2EE 릴레이 백엔드
  Shared/                # 공통 Contract DTO
deploy/
  k8s/                   # Kubernetes & Flux v2 GitOps
lib/
  Garden.Harness/        # Shared Harness (submodule)
```

<!-- HARNESS:START -->
- 시작: `lib/Garden.Harness/src/agents/AGENT_CONTEXT.md`
- 라우터: `lib/Garden.Harness/src/agents/skills/orchestrate-workflows/SKILL.md`
- 워크플로우: `lib/Garden.Harness/src/core/docs/harness-workflow.md`
- 검증: `lib/Garden.Harness/src/agents/skills/verify-completion/SKILL.md`
<!-- HARNESS:END -->
