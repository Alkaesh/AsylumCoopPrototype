# Asylum Co-op Horror Prototype (Unity 6 + Mirror)

Рабочий вертикальный срез кооперативного хоррора (1-4 игрока, listen-server) с:
- лобби (`host/join/start`);
- задачами на карте;
- ИИ босса со зрением, слухом, поиском, преследованием, атакой, переноской и подвешиванием;
- спасением с крюка и ревивом тиммейта;
- дверями (open/close/lock states + звук + сетевой sync);
- батарейками фонарика;
- динамической хоррор-атмосферой (тусклый свет, мерцания, blood-декор, scare-триггеры).

## Быстрый запуск

1. Запусти `AUTO_SETUP_PROTOTYPE.bat` из корня проекта.  
2. Дождись завершения batch-процесса.  
3. Готовый билд появится в:
   - `Builds/Windows/AsylumHorrorPrototype.exe`

Альтернативы:
- `AUTO_SETUP_PROTOTYPE_SILENT.bat` - тихий режим;
- `AUTO_SETUP_ONLY_NO_EXE.bat` - только генерация/сборка контента без EXE.

## Локальный мультиплеер-тест

1. Запусти первую копию игры, нажми `HOST`.
2. Запусти вторую копию, нажми `JOIN` и укажи `localhost`.
3. В лобби хост нажимает `START GAME`.

Быстрые батники в папке билда:
- `run_host.bat`
- `run_join.bat`

## Архитектура (основные системы)

- Сеть:
  - `Assets/Scripts/Network/HorrorNetworkManager.cs`
  - `Assets/Scripts/Network/LobbyState.cs`
- Игрок:
  - `Assets/Scripts/Player/NetworkPlayerController.cs`
  - `Assets/Scripts/Player/NetworkPlayerStatus.cs`
  - `Assets/Scripts/Player/PlayerInteractor.cs`
  - `Assets/Scripts/Player/DownedReviveInteractable.cs`
  - `Assets/Scripts/Player/PlayerFlashlight.cs`
- Босс:
  - `Assets/Scripts/Monster/MonsterAI.cs`
  - `Assets/Scripts/Monster/NoiseSystem.cs`
- Задачи/интеракции:
  - `Assets/Scripts/Tasks/*`
  - `Assets/Scripts/Interaction/NetworkInteractable.cs`
- Инициализация раунда/рандомизация:
  - `Assets/Scripts/Core/GameStateManager.cs`
  - `Assets/Scripts/Core/RoundRandomizer.cs`
  - `Assets/Scripts/Core/GameplayBootstrapper.cs`
- Генерация контента:
  - `Assets/Editor/HorrorPrototypeBuilder.cs`

## Что генерируется автоматически

Сцены:
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/Lobby.unity`
- `Assets/Scenes/HospitalLevel.unity`

Префабы:
- `Assets/Prefabs/Player.prefab`
- `Assets/Prefabs/Monster.prefab`
- `Assets/Prefabs/Generator.prefab`
- `Assets/Prefabs/Keycard.prefab`
- `Assets/Prefabs/PowerConsole.prefab`
- `Assets/Prefabs/ExitDoor.prefab`
- `Assets/Prefabs/Hook.prefab`
- `Assets/Prefabs/Door.prefab`
- `Assets/Prefabs/Battery.prefab`

## Важные изменения текущей версии

- Убрана зависимость автосборки от legacy Kenney-sync.
- Добавлен авто-ремап неподдерживаемых материалов на корректный lit-шейдер.
- Доработаны модели игрока/босса (разные силуэты и материалы).
- Двери: корректная логика открытия/закрытия + nav obstacle.
- Починены/усилены состояния carry-hook-rescue и look control в небоевых состояниях.
- Улучшена карта: тусклый свет, мерцание, blood-декор, дополнительный clutter.

## Логи автосборки

- `automation/unity_sync_packages.log`
- `automation/unity_create_prototype.log`
- `automation/unity_create_prototype_and_build.log`
