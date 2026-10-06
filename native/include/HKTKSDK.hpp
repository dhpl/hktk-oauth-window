#pragma once

#include "hktk_sdk.h"

#include <future>
#include <stdexcept>
#include <string>
#include <utility>

namespace hktk {

enum class Environment {
  Sandbox = HKTK_ENVIRONMENT_SANDBOX,
  Production = HKTK_ENVIRONMENT_PRODUCTION,
  Local = HKTK_ENVIRONMENT_LOCAL,
};

struct Config {
  Environment environment = Environment::Sandbox;
  std::string clientId;
  std::string scope = "openid profile email phone";
  std::string authorizeUrl;
  unsigned int timeoutSeconds = 300;
};

struct LoginResult {
  std::string code;
  std::string state;
  std::string redirectUri;
};

class Error : public std::runtime_error {
public:
  Error(HKTKErrorCode code, const std::string &message)
      : std::runtime_error(message), code_(code) {}

  [[nodiscard]] HKTKErrorCode code() const noexcept { return code_; }

private:
  HKTKErrorCode code_;
};

class Client final {
public:
  explicit Client(const Config &config) {
    HKTKConfig native_config{};
    native_config.environment =
        static_cast<HKTKEnvironment>(config.environment);
    native_config.client_id = config.clientId.c_str();
    native_config.scope = config.scope.c_str();
    native_config.authorize_url =
        config.authorizeUrl.empty() ? nullptr : config.authorizeUrl.c_str();
    native_config.timeout_seconds = config.timeoutSeconds;

    HKTKError error{};
    handle_ = hktk_client_create(&native_config, &error);
    if (!handle_) {
      throw Error(error.code, error.message);
    }
  }

  ~Client() { hktk_client_destroy(handle_); }

  Client(const Client &) = delete;
  Client &operator=(const Client &) = delete;

  Client(Client &&other) noexcept : handle_(other.handle_) {
    other.handle_ = nullptr;
  }

  Client &operator=(Client &&other) noexcept {
    if (this != &other) {
      hktk_client_destroy(handle_);
      handle_ = other.handle_;
      other.handle_ = nullptr;
    }
    return *this;
  }

  [[nodiscard]] LoginResult login() {
    HKTKLoginResult result{};
    HKTKError error{};
    const auto code = hktk_client_login(handle_, &result, &error);
    if (code != HKTK_OK) {
      throw Error(code, error.message);
    }

    return {result.code, result.state, result.redirect_uri};
  }

  [[nodiscard]] std::future<LoginResult> loginAsync() {
    return std::async(std::launch::async, [this] { return login(); });
  }

  void cancel() noexcept { hktk_client_cancel(handle_); }

  [[nodiscard]] static std::string version() { return hktk_sdk_version(); }

private:
  HKTKClient *handle_ = nullptr;
};

} // namespace hktk
