# syntax=docker/dockerfile:1
# Multi-stage build for the PetroVisor Lite frontend (Vite + React + TypeScript)
# NOTE: written speculatively against standard Vite conventions before Luke's
# scaffold lands. Verify package manager (npm assumed) and output dir ("dist")
# once the frontend project exists.

ARG NODE_IMAGE=node:20-alpine
ARG NGINX_IMAGE=nginx:1.27-alpine

# ---- Build stage ----
FROM ${NODE_IMAGE} AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build

# ---- Runtime stage ----
FROM ${NGINX_IMAGE} AS runtime

# SPA routing fallback config (no secrets, static config only)
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

# nginx official image already runs worker processes as non-root ("nginx" user)
CMD ["nginx", "-g", "daemon off;"]
