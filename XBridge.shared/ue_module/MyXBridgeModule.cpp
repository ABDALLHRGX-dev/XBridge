#include "MyXBridgeModule.h"
#include "Modules/ModuleManager.h"
#include "Misc/Paths.h"
#include "HAL/PlatformProcess.h"
#include "Containers/Ticker.h"
#include "Engine/Engine.h"
#include "Engine/GameInstance.h"

IMPLEMENT_MODULE(FMyXBridgeModule, MyXBridgeModule)

typedef int (*fn_init_t)(const char*);
typedef int (*fn_shutdown_t)(void);
typedef const char* (*fn_version_t)(void);
typedef int (*fn_on_create_t)(void*);
typedef int (*fn_on_start_t)(void*);
typedef int (*fn_on_tick_t)(float);
typedef int (*fn_on_level_loaded_t)(const char*);
typedef int (*fn_on_player_join_t)(const char*);
typedef int (*fn_on_command_t)(const char*);
typedef void (*fn_set_callback_t)(void(*)(const char*));

class FMyXBridgeModuleImpl : public FMyXBridgeModule
{
    void* LibHandle = nullptr;
    fn_init_t xb_init = nullptr;
    fn_shutdown_t xb_shutdown = nullptr;
    fn_version_t xb_version = nullptr;
    fn_on_create_t xb_on_create = nullptr;
    fn_on_start_t xb_on_start = nullptr;
    fn_on_tick_t xb_on_tick = nullptr;
    fn_on_level_loaded_t xb_on_level_loaded = nullptr;
    fn_on_player_join_t xb_on_player_join = nullptr;
    fn_on_command_t xb_on_command = nullptr;

public:
    virtual void StartupModule() override {
        FString BaseDir = FPaths::ProjectPluginsDir();
        FString LibName = TEXT("libxbridge_shared.so");
        FString LibPath = FPaths::Combine(*BaseDir, TEXT("xbridge_shared/Binaries/ThirdParty/"), LibName);
        LibHandle = FPlatformProcess::GetDllHandle(*LibPath);
        if (!LibHandle) return;
        xb_init = (fn_init_t)FPlatformProcess::GetDllExport(LibHandle, TEXT("xb_plugin_init"));
        xb_on_create = (fn_on_create_t)FPlatformProcess::GetDllExport(LibHandle, TEXT("xb_plugin_on_create"));
        xb_on_start = (fn_on_start_t)FPlatformProcess::GetDllExport(LibHandle, TEXT("xb_plugin_on_start"));
        xb_on_tick = (fn_on_tick_t)FPlatformProcess::GetDllExport(LibHandle, TEXT("xb_plugin_on_tick"));
        xb_on_level_loaded = (fn_on_level_loaded_t)FPlatformProcess::GetDllExport(LibHandle, TEXT("xb_plugin_on_level_loaded"));
        xb_on_player_join = (fn_on_player_join_t)FPlatformProcess::GetDllExport(LibHandle, TEXT("xb_plugin_on_player_join"));
        xb_on_command = (fn_on_command_t)FPlatformProcess::GetDllExport(LibHandle, TEXT("xb_plugin_on_command"));
        xb_shutdown = (fn_shutdown_t)FPlatformProcess::GetDllExport(LibHandle, TEXT("xb_plugin_shutdown"));
        xb_version = (fn_version_t)FPlatformProcess::GetDllExport(LibHandle, TEXT("xb_plugin_version"));
        if (xb_init) xb_init(nullptr);
        void* ue_ptr = nullptr;
        if (GEngine && GEngine->GetWorldContexts().Num() > 0) {
            UWorld* w = GEngine->GetWorldContexts()[0].World();
            if (w) ue_ptr = (void*)w->GetGameInstance();
        }
        if (xb_on_create) xb_on_create(ue_ptr);
        if (xb_on_start) xb_on_start(ue_ptr);
        FTicker::GetCoreTicker().AddTicker(FTickerDelegate::CreateRaw(this, &FMyXBridgeModuleImpl::Tick), 0.0f);
    }

    virtual void ShutdownModule() override {
        if (xb_shutdown) xb_shutdown();
        if (LibHandle) {
            FPlatformProcess::FreeDllHandle(LibHandle);
            LibHandle = nullptr;
        }
    }

    bool Tick(float DeltaSeconds) {
        if (xb_on_tick) xb_on_tick(DeltaSeconds);
        return true;
    }
};

void FMyXBridgeModule::StartupModule() {
    FModuleManager::Get().LoadModule(TEXT("MyXBridgeModule"));
}

void FMyXBridgeModule::ShutdownModule() {
}