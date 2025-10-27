#include <stdio.h>
#include <stdlib.h>
#include <dlfcn.h>
#include <unistd.h>


typedef int (*fn_init)(const char*);
typedef int (*fn_shutdown)(void);
typedef const char* (*fn_version)(void);
typedef int (*fn_on_create)(void*);
typedef int (*fn_on_start)(void*);
typedef int (*fn_on_tick)(float);
typedef int (*fn_notify)(const char*);
typedef void (*fn_set_cb)(void(*)(const char*));


static void host_cb(const char* json) {
printf("HOST CALLBACK: %s\n", json);
}


int main(void) {
void *h = dlopen("./libxbridge_shared.so", RTLD_NOW);
if (!h) return 1;
fn_init init = (fn_init)dlsym(h, "xb_plugin_init");
fn_shutdown shutdown = (fn_shutdown)dlsym(h, "xb_plugin_shutdown");
fn_version version = (fn_version)dlsym(h, "xb_plugin_version");
fn_on_create on_create = (fn_on_create)dlsym(h, "xb_plugin_on_create");
fn_on_start on_start = (fn_on_start)dlsym(h, "xb_plugin_on_start");
fn_on_tick on_tick = (fn_on_tick)dlsym(h, "xb_plugin_on_tick");
fn_notify notify = (fn_notify)dlsym(h, "xb_plugin_notify_event");
fn_set_cb setcb = (fn_set_cb)dlsym(h, "xb_plugin_set_host_callback");
if (setcb) setcb(host_cb);
if (init) init(NULL);
if (on_create) on_create(NULL);
if (on_start) on_start(NULL);
if (on_tick) on_tick(0.016f);
if (notify) notify("{\"evt\":\"example\"}");
sleep(1);
if (shutdown) shutdown();
dlclose(h);
return 0;
}