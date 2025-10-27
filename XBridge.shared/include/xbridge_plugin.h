#ifndef XBRIDGE_PLUGIN_H
#define XBRIDGE_PLUGIN_H


#ifdef __cplusplus
extern "C" {
#endif


#include <stdint.h>
#include <stddef.h>


typedef void (*xb_host_callback_t)(const char *json);


int xb_plugin_init(const char *config_json_path);
int xb_plugin_shutdown(void);
const char* xb_plugin_version(void);


int xb_plugin_on_create(void *ue_ptr);
int xb_plugin_on_start(void *ue_ptr);
int xb_plugin_on_tick(float delta_seconds);
int xb_plugin_on_level_loaded(const char *level_name);
int xb_plugin_on_player_join(const char *player_id);
int xb_plugin_on_command(const char *cmd_json);


int xb_plugin_notify_event(const char *event_json);
void xb_plugin_set_host_callback(xb_host_callback_t cb);
int xb_plugin_poll(int timeout_ms);


#ifdef __cplusplus
}
#endif


#endif