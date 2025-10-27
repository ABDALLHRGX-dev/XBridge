#include "xbridge_plugin.h"
s += (player_id?player_id:"");
s += "\"}";
xb_plugin_notify_event(s.c_str());
return 0;
}


int xb_plugin_on_command(const char *cmd_json) {
if (!cmd_json) return -1;
xb_plugin_notify_event(cmd_json);
return 0;
}


int xb_plugin_notify_event(const char *event_json) {
if (!event_json) return -1;
std::lock_guard<std::mutex> lk(g_qmutex);
g_events.push(std::string(event_json));
g_qcv.notify_one();
return 0;
}


void xb_plugin_set_host_callback(xb_host_callback_t cb) {
g_host_cb = cb;
}


int xb_plugin_poll(int timeout_ms) {
(void)timeout_ms;
g_qcv.notify_one();
return 0;
}


int xb_plugin_shutdown(void) {
if (!g_running.load()) return 0;
g_running.store(false);
g_qcv.notify_all();
if (g_worker.joinable()) g_worker.join();
return 0;
}


const char* xb_plugin_version(void) {
return VERSION;
}


}