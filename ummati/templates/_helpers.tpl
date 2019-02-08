{{/* vim: set filetype=mustache: */}}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "ummati.fullname" -}}
{{- $name := .Chart.Name -}}
{{- if contains $name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "ummati.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create default labels to be added to every resource.
*/}}
{{- define "ummati.defaultLabels" -}}
environment: {{ .Values.environment | quote }}
chart: {{ include "ummati.chart" . | quote }}
chartName: {{ .Chart.Name | quote }}
chartVersion: {{ .Chart.Version | quote }}
releaseName: {{ .Release.Name | quote }}
releaseService: {{ .Release.Service | quote }}
releaseTime: {{ date "2006-01-02_15-04-05" .Release.Time | quote }}
releaseRevision: {{ .Release.Revision | quote }}
releaseIsUpgrade: {{ .Release.IsUpgrade | quote }}
releaseIsInstall: {{ .Release.IsInstall | quote }}
{{- end -}}