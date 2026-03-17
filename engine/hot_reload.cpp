#include "hot_reload.h"

#include <iostream>

namespace fs = std::filesystem;

HotReloader::HotReloader(std::string distDir)
    : distDir_(std::move(distDir)), lastCheck_(std::chrono::steady_clock::now()),
      lastKnownWrite_(latestJsWriteTime()), pendingWrite_(fs::file_time_type::min()),
      pendingSince_(std::chrono::steady_clock::now()) {}

bool HotReloader::shouldReload() {
  const auto now = std::chrono::steady_clock::now();
  if (now - lastCheck_ < kPollInterval) {
    return false;
  }
  lastCheck_ = now;

  const auto currentWrite = latestJsWriteTime();
  if (currentWrite == fs::file_time_type::min() || currentWrite <= lastKnownWrite_) {
    return false;
  }

  if (currentWrite != pendingWrite_) {
    pendingWrite_ = currentWrite;
    pendingSince_ = now;
    return false;
  }

  if (now - pendingSince_ < kDebounce) {
    return false;
  }

  lastKnownWrite_ = pendingWrite_;
  pendingWrite_ = fs::file_time_type::min();
  return true;
}

fs::file_time_type HotReloader::latestJsWriteTime() const {
  fs::file_time_type latest = fs::file_time_type::min();

  try {
    if (!fs::exists(distDir_)) {
      return latest;
    }

    for (const auto &entry :
         fs::recursive_directory_iterator(distDir_, fs::directory_options::skip_permission_denied)) {
      if (!entry.is_regular_file()) {
        continue;
      }
      if (entry.path().extension() != ".js") {
        continue;
      }

      const auto writeTime = entry.last_write_time();
      if (writeTime > latest) {
        latest = writeTime;
      }
    }
  } catch (const std::exception &e) {
    std::cerr << "Hot reload scan error: " << e.what() << std::endl;
  }

  return latest;
}
