# Waktaverse Games Demo (Unity)

팬게임 연동을 위한 유니티 데모 프로젝트.

## Assets

### Scenes

- MenuScene : 로그인 절차, 프로필 조회, 도전과제 달성이 포함된 메인 메뉴 장면.
- SampleScene : 도전과제 달성이 포함된 게임 장면.

### Scripts

- Wakgames.cs : SDK 진입점.
- WakgamesAuth.cs : OAuth 유틸리티.
- WakgamesCallbackServer.cs : OAuth 콜백 서버.
- WakgamesDefaultTokenStorage.cs : 기본 토큰 저장소.
- IWakgamesTokenStorage.cs : 토큰 저장소 인터페이스.

## SDK 사용법

Wakgames 클래스를 이용합니다.

### 초기화

Serialize Field를 필수로 설정해야 합니다. (예제 장면 참고.)

- ClientId : 개발자 포탈에서 확인된 Client ID.
- CallbackServerPort : 개발자 포탈에서 설정한 Callback URI의 포트 번호.

선택적으로 TokenStorage를 기본 저장소 대신 별도 구현하여 설정할 수 있습니다.

### API

모든 API는 코루틴이며 콜백으로 결과를 받습니다.  
콜백 인자는 결과 데이터와 상태 코드로 구성되어 있습니다.  
API 호출이 실패한 경우 결과 데이터가 null이 되며 오류 코드가 상태 코드에 지정됩니다.  
상세한 설명은 코드 주석을 참고해주세요.

- StartLogin : 로그인 절차를 시작합니다.
- Logout : 로그아웃합니다.
- RefreshToken : 토큰을 갱신합니다. (자동으로 이뤄지므로 사용할 일 없음.)
- GetUserProfile : 사용자 프로필을 조회합니다.
- GetUnlockedAchievements : 사용자가 달성한 도전과제 목록을 얻습니다.
- UnlockAchievement : 특정 도전과제 달성을 기록합니다.

## [Package Releases](https://github.com/WakGames/Waktaverse-Games-Demo-Unity/releases)

SDK는 유니티 패키지로 배포됩니다.
