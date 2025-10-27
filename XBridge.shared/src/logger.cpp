#include <iostream>
#include <mutex>


static std::mutex log_mtx;


void xb_log(const char *msg) {
std::lock_guard<std::mutex> lk(log_mtx);
std::cerr << msg << std::endl;
}