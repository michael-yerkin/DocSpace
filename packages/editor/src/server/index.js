import express from "express";
import template from "./lib/template";
import devMiddleware from "./lib/middleware/devMiddleware";
import i18nextMiddleware from "i18next-express-middleware";
import i18next from "i18next";
import path from "path";
import compression from "compression";
import ws from "./lib/websocket";
import fs from "fs";
import logger from "morgan";
import winston from "./lib/logger.js";
import { getAssets, initDocEditor } from "./lib/helpers";
import renderApp from "./lib/helpers/render-app";
import dns from "dns";
import { translations } from "SRC_DIR/autoGeneratedTranslations";
import { getLanguage } from "@docspace/common/utils";
import { xss } from "express-xss-sanitizer";

dns.setDefaultResultOrder("ipv4first");

const fallbackLng = "en";

let port = PORT;

const config = fs.readFileSync(path.join(__dirname, "config.json"), "utf-8");
const parsedCOnfig = JSON.parse(config);

if (parsedCOnfig?.PORT) {
  port = parsedCOnfig.PORT;
}

winston.stream = {
  write: (message) => winston.info(message),
};

const app = express();

const resources = {};

translations.forEach((lngCol, key) => {
  resources[key] = {};
  lngCol.forEach((data, ns) => {
    if (resources[key]) {
      resources[key][ns] = data;
    }
  });
});

i18next.init({
  fallbackLng: fallbackLng,
  load: "currentOnly",

  saveMissing: true,
  ns: ["Editor", "Common"],
  defaultNS: "Editor",

  resources,

  interpolation: {
    escapeValue: false,
    format: function (value, format) {
      if (format === "lowercase") return value.toLowerCase();
      return value;
    },
  },
});

app.use(i18nextMiddleware.handle(i18next));
app.use(compression());
app.use(xss());
app.use(
  "/doceditor/",
  express.static(path.resolve(path.join(__dirname, "client")), {
    // don`t delete
    // https://github.com/pillarjs/send/issues/110
    cacheControl: false,
  })
);
app.use(logger("dev", { stream: winston.stream }));

if (IS_DEVELOPMENT) {
  app.use(devMiddleware);

  app.get("/health", (req, res) => {
    res.send({ status: "Healthy" });
  });

  app.get("/doceditor", async (req, res) => {
    const { i18n, initialEditorState, assets } = req;
    const userLng = getLanguage(initialEditorState?.user?.cultureName) || "en";

    await i18next.changeLanguage(userLng);

    let initialI18nStoreASC = {};

    if (i18n && i18n?.services?.resourceStore?.data) {
      for (let key in i18n?.services?.resourceStore?.data) {
        if (key === "en" || key === userLng) {
          initialI18nStoreASC[key] = i18n.services.resourceStore.data[key];
        }
      }
    }

    if (initialEditorState?.error) {
      winston.error(initialEditorState.error.errorMessage);
    }

    const { component, styleTags } = renderApp(i18n, initialEditorState);

    const htmlString = template(
      initialEditorState,
      component,
      styleTags,
      initialI18nStoreASC,
      userLng,
      assets
    );

    res.send(htmlString);
  });

  const server = app.listen(port, () => {
    winston.info(`Server is listening on port ${port}`);
  });

  const wss = ws(server);

  const manifestFile = path.resolve(
    path.join(__dirname, "client/manifest.json")
  );

  let fsWait = false;
  let waitTimeout = null;
  fs.watch(manifestFile, (event, filename) => {
    if (filename && event === "change") {
      if (fsWait) return;
      fsWait = true;
      waitTimeout = setTimeout(() => {
        fsWait = false;
        clearTimeout(waitTimeout);
        wss.broadcast("reload");
      }, 100);
    }
  });
} else {
  let assets;

  try {
    assets = getAssets();
  } catch (e) {
    winston.error(e.message);
  }

  app.get("/health", (req, res) => {
    res.send({ status: "Healthy" });
  });

  app.get("/doceditor", async (req, res) => {
    const { i18n } = req;
    let initialEditorState;

    try {
      initialEditorState = await initDocEditor(req);
    } catch (e) {
      winston.error(e.message);
    }

    const userLng = getLanguage(initialEditorState?.user?.cultureName) || "en";

    await i18next.changeLanguage(userLng);

    let initialI18nStoreASC = {};

    if (i18n && i18n?.services?.resourceStore?.data) {
      for (let key in i18n?.services?.resourceStore?.data) {
        if (key === "en" || key === userLng) {
          initialI18nStoreASC[key] = i18n.services.resourceStore.data[key];
        }
      }
    }

    if (initialEditorState?.error) {
      winston.error(initialEditorState.error.errorMessage);
    }

    const { component, styleTags } = renderApp(i18n, initialEditorState);

    const htmlString = template(
      initialEditorState,
      component,
      styleTags,
      initialI18nStoreASC,
      userLng,
      assets
    );

    res.send(htmlString);
  });

  app.listen(port, () => {
    winston.info(`Server is listening on port ${port}`);
  });
}
