# Waktaverse Games SDK (Unity)

Unity 게임을 Waktaverse Games와 연동하기 위한 클라이언트 모듈 및 사용 예제입니다.

## Assets

### Scenes

- MenuScene : 로그인 절차, 프로필 조회, 도전과제 달성이 포함된 메인 메뉴 장면.
- SampleScene : 도전과제 달성이 포함된 게임 장면.

### Scripts

- [Wakgames.cs](<https://github.com/WakGames/Waktaverse-Games-Demo-Unity/blob/main/Assets/Wakgames/Scripts/Wakgames.cs>) : SDK 진입점.
- [WakgamesAchieve.cs](<https://github.com/WakGames/Waktaverse-Games-Demo-Unity/blob/main/Assets/Wakgames/Scripts/WakgamesAchieve.cs>) : 도전과제 알림 관리.
- [AchievePanel.cs](<https://github.com/WakGames/Waktaverse-Games-Demo-Unity/blob/main/Assets/Wakgames/Scripts/AchievePanel.cs>) : 도전과제 알림.
- [WakgamesAuth.cs](<https://github.com/WakGames/Waktaverse-Games-Demo-Unity/blob/main/Assets/Wakgames/Scripts/WakgamesAuth.cs>) : OAuth 유틸리티.
- [WakgamesCallbackServer.cs](<https://github.com/WakGames/Waktaverse-Games-Demo-Unity/blob/main/Assets/Wakgames/Scripts/WakgamesCallbackServer.cs>) : OAuth 콜백 서버.
- [WakgamesDefaultTokenStorage.cs](<https://github.com/WakGames/Waktaverse-Games-Demo-Unity/blob/main/Assets/Wakgames/Scripts/DefaultWakgamesTokenStorage.cs>) : 기본 토큰 저장소.
- [IWakgamesTokenStorage.cs](<https://github.com/WakGames/Waktaverse-Games-Demo-Unity/blob/main/Assets/Wakgames/Scripts/IWakgamesTokenStorage.cs>) : 토큰 저장소 인터페이스.

## SDK 사용법

Wakgames 클래스를 이용합니다.

### 초기화

상단 Window바의 Tools/Wakgames를 클릭하여 Waktgames Setup창을 띄웁니다. (예제 장면 참고.)

- 클라이언트 ID : 개발자 포탈에서 확인된 Client ID.
- 게임 서버 포트 : 개발자 포탈에서 설정한 Callback URI의 포트 번호.  
  (개발자 포탈에서 무조건 `http://localhost:포트/callback` 형태로 설정해주세요.)
- 도전과제 알림 : 도전과제 알림창의 팝업 여부를 결정합니다.
- 도전과제 SFX : 도전과제 알림창 팝업시 효과음 출력 여부를 결정합니다.
- 도전과제 위치 : 도전과제 알림창 팝업시 화면에서 팝업되는 위치를 결정합니다.
변경 후 꼭 "변경사항 저장" 버튼을 클릭하셔야합니다.
(ID가 누락될 경우, 서버 포트가 누락될 경우, 서버 포트가 숫자가 아닐 경우 저장되지 않습니다.)

선택적으로 TokenStorage를 기본 저장소 대신 별도 구현하여 설정할 수 있습니다.
(기본은 DefaultWakgamesTokenStorage로 PlayerPrefs를 사용합니다.)

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
- GetStats : 사용자의 통계 값들을 얻습니다.
- SetStats : 사용자의 대상 통계 값들을 입력합니다.
- GetStatBoard : 대상 통계의 전체 사용자 값을 내림차순으로 얻습니다.

## [Package Releases](https://github.com/WakGames/Waktaverse-Games-Demo-Unity/releases)

SDK는 유니티 패키지로 배포됩니다.
